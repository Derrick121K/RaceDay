using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Route("organiser")]
public sealed class OrganiserController : ProtectedPageController
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api) : base(api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var eventsResult = await _api.GetEventsAsync(cancellationToken);
        if (!eventsResult.IsSuccess || eventsResult.Value is null)
            return ApiFailure(eventsResult);

        var ownedEvents = eventsResult.Value
            .Where(raceEvent => raceEvent.OrganiserID == CurrentProfile.UserId)
            .OrderBy(raceEvent => raceEvent.EventDate)
            .ToList();
        var enrolmentCount = 0;

        foreach (var raceEvent in ownedEvents)
        {
            var enrolments = await _api.GetEventEnrolmentsAsync(
                raceEvent.EventID,
                cancellationToken);
            if (!enrolments.IsSuccess || enrolments.Value is null)
                return ApiFailure(enrolments);

            enrolmentCount += enrolments.Value.Count;
        }

        var upcoming = ownedEvents
            .Where(raceEvent => raceEvent.EventDate.Date >= DateTime.Today)
            .Take(4)
            .ToList();

        return View(new OrganiserDashboardViewModel
        {
            EventCount = ownedEvents.Count,
            UpcomingEventCount = ownedEvents.Count(raceEvent =>
                raceEvent.EventDate.Date >= DateTime.Today),
            EnrolmentCount = enrolmentCount,
            UpcomingEvents = upcoming
        });
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var result = await _api.GetEventsAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return ApiFailure(result);

        return View(new OrganiserEventsViewModel
        {
            Events = result.Value.OrderBy(raceEvent => raceEvent.EventDate).ToList(),
            OrganiserId = CurrentProfile.UserId
        });
    }

    [HttpGet("events/create")]
    public async Task<IActionResult> CreateEvent(CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var model = new OrganiserEventFormViewModel { EventDate = DateTime.Today };
        var typeFailure = await LoadEventTypesAsync(model, cancellationToken);
        return typeFailure ?? View("EventForm", model);
    }

    [HttpPost("events/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(
        OrganiserEventFormViewModel model,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        ValidateEventDate(model);
        if (!ModelState.IsValid)
            return await ReturnEventFormAsync(model, cancellationToken);

        var result = await _api.CreateEventAsync(ToEventRequest(model), cancellationToken);
        if (!result.IsSuccess)
            return await ReturnEventFormForApiErrorAsync(model, result, cancellationToken);

        if (model.Banner is { Length: > 0 })
        {
            var imageResult = await _api.UploadImageAsync(
                $"api/events/{result.Value!.EventID}/banner", model.Banner, cancellationToken);
            if (!imageResult.IsSuccess)
                TempData["WarningMessage"] = "The event was saved, but its banner could not be uploaded.";
        }

        TempData["SuccessMessage"] = "Event created successfully.";
        return RedirectToAction(nameof(EventDetails), new { id = result.Value!.EventID });
    }

    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> EventDetails(int id, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var result = await _api.GetEventAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return ApiFailure(result);

        return View(new OrganiserEventDetailsViewModel
        {
            Event = result.Value,
            IsOwner = result.Value.OrganiserID == CurrentProfile.UserId
        });
    }

    [HttpGet("events/{id:int}/edit")]
    public async Task<IActionResult> EditEvent(int id, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(id, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        var model = new OrganiserEventFormViewModel
        {
            EventId = raceEvent!.EventID,
            EventName = raceEvent.EventName,
            Description = raceEvent.Description ?? string.Empty,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            Distance = raceEvent.Distance,
            EventTypeId = raceEvent.EventTypeID,
            BannerImageAvailable = !string.IsNullOrWhiteSpace(raceEvent.BannerImagePath)
        };

        var typeFailure = await LoadEventTypesAsync(model, cancellationToken);
        return typeFailure ?? View("EventForm", model);
    }

    [HttpPost("events/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        int id,
        OrganiserEventFormViewModel model,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(id, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        model.EventId = id;
        ValidateEventDate(model);
        if (!ModelState.IsValid)
        {
            model.EventName = model.EventName.Trim();
            model.Description = model.Description.Trim();
            model.Location = model.Location.Trim();
            return await ReturnEventFormAsync(model, cancellationToken);
        }

        var result = await _api.UpdateEventAsync(id, ToEventRequest(model), cancellationToken);
        if (!result.IsSuccess)
            return await ReturnEventFormForApiErrorAsync(model, result, cancellationToken);

        if (model.Banner is { Length: > 0 })
        {
            var imageResult = await _api.UploadImageAsync(
                $"api/events/{id}/banner", model.Banner, cancellationToken);

            if (!imageResult.IsSuccess)
            {
                TempData["WarningMessage"] =
                    "Event details were saved, but the banner could not be uploaded.";

                return RedirectToAction(nameof(EventDetails), new { id });
            }

            TempData["SuccessMessage"] = "Event details and banner updated.";
        }
        else
        {
            TempData["SuccessMessage"] = "Event details updated.";
        }

        return RedirectToAction(nameof(EventDetails), new { id });
    }

    private async Task<IActionResult?> LoadEventTypesAsync(
        OrganiserEventFormViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await _api.GetEventTypesAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return ApiFailure(result);

        model.EventTypes = result.Value;
        return null;
    }

    private async Task<IActionResult> ReturnEventFormAsync(
        OrganiserEventFormViewModel model,
        CancellationToken cancellationToken)
    {
        var failure = await LoadEventTypesAsync(model, cancellationToken);
        return failure ?? View("EventForm", model);
    }

    private async Task<IActionResult> ReturnEventFormForApiErrorAsync(
        OrganiserEventFormViewModel model,
        ApiCallResult<EventResponse> result,
        CancellationToken cancellationToken)
    {
        if (result.StatusCode is System.Net.HttpStatusCode.Unauthorized or
            System.Net.HttpStatusCode.Forbidden)
            return ApiFailure(result);

        ModelState.AddModelError(string.Empty,
            result.ErrorMessage ?? "The event could not be saved.");
        return await ReturnEventFormAsync(model, cancellationToken);
    }

    private static EventRequest ToEventRequest(OrganiserEventFormViewModel model) => new()
    {
        EventName = model.EventName.Trim(),
        Description = model.Description.Trim(),
        EventDate = model.EventDate.Date,
        Location = model.Location.Trim(),
        Distance = model.Distance,
        EventTypeId = model.EventTypeId
    };

    private void ValidateEventDate(OrganiserEventFormViewModel model)
    {
        if (model.EventDate == default)
            ModelState.AddModelError(nameof(model.EventDate), "Choose an event date.");
    }

    [HttpGet("events/{id:int}/banner-image")]
    public async Task<IActionResult> BannerImage(int id, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(id, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;
        if (string.IsNullOrWhiteSpace(raceEvent!.BannerImagePath))
            return NotFound();

        var image = await _api.GetImageAsync(
            $"api/events/{id}/banner-image", cancellationToken);
        if (!image.IsSuccess || image.Value is null)
            return NotFound();

        return File(image.Value.Bytes, image.Value.ContentType);
    }

}

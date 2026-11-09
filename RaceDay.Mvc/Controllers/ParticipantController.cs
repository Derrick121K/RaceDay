using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Route("participant")]
public sealed class ParticipantController : ProtectedPageController
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api) : base(api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;

        var eventsTask = _api.GetEventsAsync(cancellationToken);
        var enrolmentsTask = _api.GetMyEnrolmentsAsync(cancellationToken);
        var resultsTask = _api.GetMyResultsAsync(cancellationToken);
        await Task.WhenAll(eventsTask, enrolmentsTask, resultsTask);
        if (!eventsTask.Result.IsSuccess || eventsTask.Result.Value is null) return ApiFailure(eventsTask.Result);
        if (!enrolmentsTask.Result.IsSuccess || enrolmentsTask.Result.Value is null) return ApiFailure(enrolmentsTask.Result);
        if (!resultsTask.Result.IsSuccess || resultsTask.Result.Value is null) return ApiFailure(resultsTask.Result);

        return View(new ParticipantDashboardViewModel
        {
            Profile = CurrentProfile,
            UpcomingEvents = eventsTask.Result.Value
                .Where(e => e.EventDate.Date >= DateTime.Today)
                .OrderBy(e => e.EventDate)
                .Take(4).ToList(),
            EnrolmentCount = enrolmentsTask.Result.Value.Count,
            RecentEnrolments = enrolmentsTask.Result.Value.Take(4).ToList(),
            RecentResults = resultsTask.Result.Value.Take(4).ToList()
        });
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(string? search, CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;

        var eventsTask = _api.GetEventsAsync(cancellationToken);
        var enrolmentsTask = _api.GetMyEnrolmentsAsync(cancellationToken);
        await Task.WhenAll(eventsTask, enrolmentsTask);
        if (!eventsTask.Result.IsSuccess || eventsTask.Result.Value is null) return ApiFailure(eventsTask.Result);
        if (!enrolmentsTask.Result.IsSuccess || enrolmentsTask.Result.Value is null) return ApiFailure(enrolmentsTask.Result);

        var filtered = eventsTask.Result.Value.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(e => Contains(e.EventName, term) ||
                Contains(e.Location, term) || Contains(e.EventTypeName, term));
        }

        return View(new ParticipantEventsViewModel
        {
            Search = search,
            Events = filtered.OrderBy(e => e.EventDate).ToList(),
            EnrolledEventIds = enrolmentsTask.Result.Value.Select(e => e.EventID).ToHashSet()
        });
    }

    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> EventDetails(int id, CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;

        var eventTask = _api.GetEventAsync(id, cancellationToken);
        var categoriesTask = _api.GetEventCategoriesAsync(id, cancellationToken);
        var enrolmentsTask = _api.GetMyEnrolmentsAsync(cancellationToken);
        await Task.WhenAll(eventTask, categoriesTask, enrolmentsTask);
        if (!eventTask.Result.IsSuccess || eventTask.Result.Value is null) return ApiFailure(eventTask.Result);
        if (!categoriesTask.Result.IsSuccess || categoriesTask.Result.Value is null) return ApiFailure(categoriesTask.Result);
        if (!enrolmentsTask.Result.IsSuccess || enrolmentsTask.Result.Value is null) return ApiFailure(enrolmentsTask.Result);

        var enrolment = enrolmentsTask.Result.Value.FirstOrDefault(e => e.EventID == id);
        return View(new ParticipantEventDetailsViewModel
        {
            Event = eventTask.Result.Value,
            Categories = categoriesTask.Result.Value,
            EnrolmentStatus = enrolment?.Status
        });
    }

    [HttpGet("events/{id:int}/enrol")]
    public async Task<IActionResult> Enrol(int id, CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;
        var (model, error) = await LoadEnrolmentFormAsync(id, cancellationToken);
        return error ?? View("Enrol", model);
    }

    [HttpPost("events/{id:int}/enrol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, ParticipantEnrolmentFormViewModel model, CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;
        model.EventId = id;
        if (!ModelState.IsValid)
        {
            var (invalidModel, loadError) = await LoadEnrolmentFormAsync(id, cancellationToken);
            if (loadError is not null) return loadError;
            invalidModel!.CategoryId = model.CategoryId;
            return View("Enrol", invalidModel);
        }

        var result = await _api.CreateEnrolmentAsync(
            new EnrolmentRequest { EventId = id, CategoryId = model.CategoryId }, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                return ApiFailure(result);
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "You could not be enrolled in this event.");
            var (retryModel, loadError) = await LoadEnrolmentFormAsync(id, cancellationToken);
            return loadError ?? View("Enrol", retryModel);
        }

        TempData["SuccessMessage"] = "You are enrolled in this event.";
        return RedirectToAction(nameof(EventDetails), new { id });
    }

    [HttpGet("enrolments")]
    public async Task<IActionResult> Enrolments(CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;
        var enrolments = await _api.GetMyEnrolmentsAsync(cancellationToken);
        if (!enrolments.IsSuccess || enrolments.Value is null) return ApiFailure(enrolments);
        var events = await _api.GetEventsAsync(cancellationToken);
        if (!events.IsSuccess || events.Value is null) return ApiFailure(events);

        var categoriesByEvent = new Dictionary<int, IReadOnlyList<CategoryResponse>>();
        foreach (var eventId in enrolments.Value.Select(e => e.EventID).Distinct())
        {
            var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
            if (!categories.IsSuccess || categories.Value is null) return ApiFailure(categories);
            categoriesByEvent[eventId] = categories.Value;
        }

        var items = enrolments.Value.Select(enrolment => new ParticipantEnrolmentItemViewModel
        {
            Enrolment = enrolment,
            Event = events.Value.FirstOrDefault(e => e.EventID == enrolment.EventID),
            Category = categoriesByEvent.GetValueOrDefault(enrolment.EventID, [])
                .FirstOrDefault(c => c.CategoryID == enrolment.CategoryID)
        }).ToList();
        return View(new ParticipantEnrolmentsViewModel { Enrolments = items });
    }

    [HttpGet("results")]
    public async Task<IActionResult> Results(CancellationToken cancellationToken)
    {
        var failure = await RequireParticipantAsync(cancellationToken);
        if (failure is not null) return failure;
        var result = await _api.GetMyResultsAsync(cancellationToken);
        if (!result.IsSuccess || result.Value is null) return ApiFailure(result);
        return View(new ParticipantResultsViewModel { Results = result.Value });
    }

    private async Task<(ParticipantEnrolmentFormViewModel? Model, IActionResult? Failure)> LoadEnrolmentFormAsync(int eventId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetEventAsync(eventId, cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Value is null) return (null, ApiFailure(eventResult));
        var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
        if (!categories.IsSuccess || categories.Value is null) return (null, ApiFailure(categories));
        var enrolled = await _api.GetMyEnrolmentsAsync(cancellationToken);
        if (!enrolled.IsSuccess || enrolled.Value is null) return (null, ApiFailure(enrolled));
        if (enrolled.Value.Any(e => e.EventID == eventId))
        {
            TempData["ErrorMessage"] = "You are already enrolled in this event.";
            return (null, RedirectToAction(nameof(EventDetails), new { id = eventId }));
        }
        return (new ParticipantEnrolmentFormViewModel
        {
            EventId = eventId,
            EventName = eventResult.Value.EventName,
            Categories = categories.Value
        }, null);
    }

    private static bool Contains(string? value, string term) =>
        value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
}

using System.Globalization;
using RaceDay.Contracts;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers;

[Route("organiser/events/{eventId:int}")]
public sealed class OrganiserRaceController : ProtectedPageController
{
    private readonly RaceDayApiClient _api;

    public OrganiserRaceController(RaceDayApiClient api) : base(api) => _api = api;

    [HttpGet("enrolments")]
    public async Task<IActionResult> Enrolments(int eventId, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null) return failure;
        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null) return eventFailure;

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId, cancellationToken);
        if (!enrolments.IsSuccess || enrolments.Value is null) return ApiFailure(enrolments);
        var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
        if (!categories.IsSuccess || categories.Value is null) return ApiFailure(categories);

        return View(new OrganiserEnrolmentsViewModel
        {
            Event = raceEvent!,
            Enrolments = enrolments.Value,
            CategoryNames = categories.Value.ToDictionary(c => c.CategoryID, c => c.CategoryName)
        });
    }

    [HttpPost("enrolments/{enrolmentId:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int eventId, int enrolmentId, string status, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null) return failure;
        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null) return eventFailure;
        var enrolment = await _api.GetEnrolmentAsync(enrolmentId, cancellationToken);
        if (!enrolment.IsSuccess || enrolment.Value is null) return ApiFailure(enrolment);
        if (enrolment.Value.EventId != eventId) return NotFound();
        var normalized = status?.Trim().ToUpperInvariant();
        if (normalized is not ("ACTIVE" or "CANCELLED" or "COMPLETED"))
        {
            TempData["ErrorMessage"] = "Choose a valid enrolment status.";
            return RedirectToAction(nameof(Enrolments), new { eventId });
        }
        var result = await _api.UpdateEnrolmentStatusAsync(enrolmentId,
            new EnrolmentStatusRequest { Status = normalized }, cancellationToken);
        if (!result.IsSuccess) return ApiFailure(result);
        TempData["SuccessMessage"] = "Enrolment status updated.";
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    [HttpGet("results")]
    public async Task<IActionResult> Results(int eventId, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null) return failure;
        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null) return eventFailure;
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId, cancellationToken);
        if (!enrolments.IsSuccess || enrolments.Value is null) return ApiFailure(enrolments);
        var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
        if (!categories.IsSuccess || categories.Value is null) return ApiFailure(categories);
        var categoryNames = categories.Value.ToDictionary(c => c.CategoryID, c => c.CategoryName);
        var rows = new List<OrganiserResultRowViewModel>();
        foreach (var enrolment in enrolments.Value)
        {
            var result = await _api.GetEnrolmentResultAsync(enrolment.EnrolmentID, cancellationToken);
            if (!result.IsSuccess && result.StatusCode != System.Net.HttpStatusCode.NotFound)
                return ApiFailure(result);
            rows.Add(new OrganiserResultRowViewModel
            {
                Enrolment = enrolment,
                CategoryName = categoryNames.GetValueOrDefault(enrolment.CategoryID, "Category"),
                Result = result.Value
            });
        }
        return View(new OrganiserResultsViewModel { Event = raceEvent!, Results = rows });
    }

    [HttpGet("results/{enrolmentId:int}/edit")]
    public async Task<IActionResult> EditResult(int eventId, int enrolmentId, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null) return failure;
        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null) return eventFailure;
        var enrolment = await _api.GetEnrolmentAsync(enrolmentId, cancellationToken);
        if (!enrolment.IsSuccess || enrolment.Value is null) return ApiFailure(enrolment);
        if (enrolment.Value.EventId != eventId) return NotFound();
        var result = await _api.GetEnrolmentResultAsync(enrolmentId, cancellationToken);
        if (!result.IsSuccess && result.StatusCode != System.Net.HttpStatusCode.NotFound) return ApiFailure(result);
        return View("ResultForm", new OrganiserResultFormViewModel
        {
            EventId = eventId, EnrolmentId = enrolmentId, EventName = raceEvent!.EventName,
            ParticipantId = enrolment.Value.ParticipantId, ResultId = result.Value?.ResultID,
            FinishTime = result.Value is null ? "" : FormatFinishTime(result.Value.FinishTime),
            FinishingPosition = result.Value?.FinishingPosition ?? 0,
            HasResult = result.Value is not null,
            CategoryName = enrolment.Value.CategoryName
        });
    }

    [HttpPost("results/{enrolmentId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditResult(int eventId, int enrolmentId, OrganiserResultFormViewModel model, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null) return failure;
        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null) return eventFailure;
        var enrolment = await _api.GetEnrolmentAsync(enrolmentId, cancellationToken);
        if (!enrolment.IsSuccess || enrolment.Value is null) return ApiFailure(enrolment);
        if (enrolment.Value.EventId != eventId) return NotFound();
        model.EventId = eventId; model.EnrolmentId = enrolmentId;
        model.EventName = raceEvent!.EventName; model.ParticipantId = enrolment.Value.ParticipantId;
        if (!TimeSpan.TryParse(model.FinishTime, CultureInfo.InvariantCulture, out var finishTime) || finishTime < TimeSpan.Zero)
            ModelState.AddModelError(nameof(model.FinishTime), "Enter a valid non-negative finish time.");
        if (!ModelState.IsValid) return View("ResultForm", model);
        var existing = await _api.GetEnrolmentResultAsync(enrolmentId, cancellationToken);
        if (!existing.IsSuccess && existing.StatusCode != System.Net.HttpStatusCode.NotFound) return ApiFailure(existing);
        var request = new ResultRequest { FinishTime = finishTime, FinishingPosition = model.FinishingPosition };
        var saved = existing.Value is null
            ? await _api.CreateResultAsync(enrolmentId, request, cancellationToken)
            : await _api.UpdateResultAsync(enrolmentId, request, cancellationToken);
        if (!saved.IsSuccess) return ApiFailure(saved);
        TempData["SuccessMessage"] = "Race result saved.";
        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpPost("results/{enrolmentId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResult(int eventId, int enrolmentId, CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null) return failure;
        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null) return eventFailure;
        var enrolment = await _api.GetEnrolmentAsync(enrolmentId, cancellationToken);
        if (!enrolment.IsSuccess || enrolment.Value is null) return ApiFailure(enrolment);
        if (enrolment.Value.EventId != eventId) return NotFound();
        var deleted = await _api.DeleteResultAsync(enrolmentId, cancellationToken);
        if (!deleted.IsSuccess) return ApiFailure(deleted);
        TempData["SuccessMessage"] = "Race result removed.";
        return RedirectToAction(nameof(Results), new { eventId });
    }

    private static string FormatFinishTime(TimeSpan finishTime) =>
        $"{(long)finishTime.TotalHours:00}:{finishTime.Minutes:00}:{finishTime.Seconds:00}";
}

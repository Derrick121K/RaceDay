using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Route("organiser")]
public sealed class OrganiserCategoriesController : ProtectedPageController
{
    private readonly RaceDayApiClient _api;

    public OrganiserCategoriesController(RaceDayApiClient api) : base(api)
    {
        _api = api;
    }

    [HttpGet("events/{eventId:int}/categories")]
    public async Task<IActionResult> Categories(
        int eventId,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
        if (!categories.IsSuccess || categories.Value is null)
            return ApiFailure(categories);

        return View(new OrganiserCategoriesViewModel
        {
            Event = raceEvent!,
            Categories = categories.Value
        });
    }

    [HttpGet("events/{eventId:int}/categories/create")]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        return View("CategoryForm", new OrganiserCategoryFormViewModel
        {
            EventId = eventId,
            EventName = raceEvent!.EventName
        });
    }

    [HttpPost("events/{eventId:int}/categories/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        OrganiserCategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        model.EventId = eventId;
        model.EventName = raceEvent!.EventName;
        ValidateAgeRange(model);
        if (!ModelState.IsValid)
            return View("CategoryForm", model);

        var result = await _api.CreateCategoryAsync(eventId, ToRequest(model), cancellationToken);
        if (!result.IsSuccess)
            return ReturnCategoryFormForApiError(model, result);

        TempData["SuccessMessage"] = "Category created.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet("events/{eventId:int}/categories/{categoryId:int}/edit")]
    public async Task<IActionResult> EditCategory(
        int eventId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
        if (!categories.IsSuccess || categories.Value is null)
            return ApiFailure(categories);

        var category = categories.Value.SingleOrDefault(item => item.CategoryID == categoryId);
        if (category is null)
            return NotFound();

        return View("CategoryForm", new OrganiserCategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = category.CategoryID,
            EventName = raceEvent!.EventName,
            CategoryName = category.CategoryName,
            Description = category.Description,
            MinimumAge = category.MinimumAge,
            MaximumAge = category.MaximumAge
        });
    }

    [HttpPost("events/{eventId:int}/categories/{categoryId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(
        int eventId,
        int categoryId,
        OrganiserCategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        model.EventId = eventId;
        model.CategoryId = categoryId;
        model.EventName = raceEvent!.EventName;
        ValidateAgeRange(model);
        if (!ModelState.IsValid)
            return View("CategoryForm", model);

        var result = await _api.UpdateCategoryAsync(categoryId, ToRequest(model), cancellationToken);
        if (!result.IsSuccess)
            return ReturnCategoryFormForApiError(model, result);

        TempData["SuccessMessage"] = "Category updated.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost("events/{eventId:int}/categories/{categoryId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(
        int eventId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        var failure = await RequireOrganiserAsync(cancellationToken);
        if (failure is not null)
            return failure;

        var (raceEvent, eventFailure) = await RequireOwnedEventAsync(eventId, cancellationToken);
        if (eventFailure is not null)
            return eventFailure;

        var categories = await _api.GetEventCategoriesAsync(eventId, cancellationToken);
        if (!categories.IsSuccess || categories.Value is null)
            return ApiFailure(categories);
        if (!categories.Value.Any(category => category.CategoryID == categoryId))
            return NotFound();

        var result = await _api.DeleteCategoryAsync(categoryId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.StatusCode is System.Net.HttpStatusCode.Unauthorized or
                System.Net.HttpStatusCode.Forbidden)
                return ApiFailure(result);

            TempData["ErrorMessage"] = result.ErrorMessage ??
                "This category could not be deleted. It may have enrolments.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        TempData["SuccessMessage"] = "Category deleted.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    private IActionResult ReturnCategoryFormForApiError(
        OrganiserCategoryFormViewModel model,
        ApiCallResult<CategoryResponse> result)
    {
        if (result.StatusCode is System.Net.HttpStatusCode.Unauthorized or
            System.Net.HttpStatusCode.Forbidden)
            return ApiFailure(result);

        ModelState.AddModelError(string.Empty,
            result.ErrorMessage ?? "The category could not be saved.");
        return View("CategoryForm", model);
    }

    private void ValidateAgeRange(OrganiserCategoryFormViewModel model)
    {
        if (model.MinimumAge.HasValue && model.MaximumAge.HasValue &&
            model.MaximumAge.Value < model.MinimumAge.Value)
        {
            ModelState.AddModelError(nameof(model.MaximumAge),
                "Maximum age cannot be less than minimum age.");
        }
    }

    private static CategoryRequest ToRequest(OrganiserCategoryFormViewModel model) => new()
    {
        CategoryName = model.CategoryName.Trim(),
        Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim(),
        MinimumAge = model.MinimumAge,
        MaximumAge = model.MaximumAge
    };
}

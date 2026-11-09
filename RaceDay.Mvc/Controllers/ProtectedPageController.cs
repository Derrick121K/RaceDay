using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public abstract class ProtectedPageController : Controller
{
    public const string CurrentProfileItemKey = "RaceDay.CurrentProfile";

    private readonly RaceDayApiClient _api;

    protected ProtectedPageController(RaceDayApiClient api)
    {
        _api = api;
    }

    protected UserProfileResponse CurrentProfile { get; private set; } = null!;

    protected IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/Forbidden.cshtml");
    }

    protected IActionResult ApiFailure<T>(ApiCallResult<T> result)
    {
        if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            TempData["ErrorMessage"] = "Your session has expired. Please sign in again.";
            return RedirectToAction("Login", "Account");
        }

        if (result.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return AccessDenied();

        if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();

        Response.StatusCode = result.IsConnectionFailure
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status502BadGateway;
        return View("~/Views/Shared/ApiUnavailable.cshtml",
            result.ErrorMessage ?? "The RaceDay API could not complete this request.");
    }

    protected async Task<(EventResponse? Event, IActionResult? Failure)> RequireOwnedEventAsync(
        int eventId,
        CancellationToken cancellationToken)
    {
        var result = await _api.GetEventAsync(eventId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return (null, ApiFailure(result));

        if (result.Value.OrganiserID != CurrentProfile.UserId)
            return (null, AccessDenied());

        return (result.Value, null);
    }

    protected async Task<IActionResult?> RequireProfileAsync(
        CancellationToken cancellationToken)
    {
        var result = await _api.GetCurrentProfileAsync(cancellationToken);
        if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            TempData["ErrorMessage"] =
                "Your session has expired. Please sign in again.";
            return RedirectToAction("Login", "Account");
        }

        if (result.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return AccessDenied();
        }

        if (result.IsConnectionFailure)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("~/Views/Shared/ApiUnavailable.cshtml", result.ErrorMessage);
        }

        if (!result.IsSuccess || result.Value is null)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            return View("~/Views/Shared/ApiUnavailable.cshtml",
                result.ErrorMessage ?? "The API could not load this page.");
        }

        CurrentProfile = result.Value;
        ViewData["CurrentUser"] = CurrentProfile;
        HttpContext.Items[CurrentProfileItemKey] = CurrentProfile;
        return null;
    }

    protected async Task<IActionResult?> RequireOrganiserAsync(
        CancellationToken cancellationToken)
    {
        var failure = await RequireProfileAsync(cancellationToken);
        if (failure is not null)
            return failure;

        return string.Equals(CurrentProfile.Role, "ORGANISER", StringComparison.OrdinalIgnoreCase)
            ? null
            : AccessDenied();
    }
}

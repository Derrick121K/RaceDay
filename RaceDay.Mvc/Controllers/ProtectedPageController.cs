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
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("~/Views/Shared/Forbidden.cshtml");
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
}

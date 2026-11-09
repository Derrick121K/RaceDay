using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Controllers;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.ViewComponents;

public sealed class MainNavigationViewComponent : ViewComponent
{
    private readonly RaceDayApiClient _api;

    public MainNavigationViewComponent(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        UserProfileResponse? profile = null;
        if (HttpContext.Items.TryGetValue(
                ProtectedPageController.CurrentProfileItemKey,
                out var currentProfile))
        {
            profile = currentProfile as UserProfileResponse;
        }
        else if (HttpContext.Request.Cookies.ContainsKey(
                     ApiSessionCookieHandler.SessionCookieName))
        {
            var result = await _api.GetCurrentProfileAsync(HttpContext.RequestAborted);
            if (result.IsSuccess)
                profile = result.Value;
        }

        return View(profile);
    }
}

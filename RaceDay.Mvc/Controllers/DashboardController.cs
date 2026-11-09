using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class DashboardController : ProtectedPageController
{
    public DashboardController(RaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var failure = await RequireProfileAsync(cancellationToken);
        if (failure is not null) return failure;
        if (string.Equals(CurrentProfile.Role, "PARTICIPANT", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Participant");
        if (string.Equals(CurrentProfile.Role, "ORGANISER", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Organiser");
        return AccessDenied();
    }
}

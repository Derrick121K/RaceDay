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
        return failure ?? View(CurrentProfile);
    }
}

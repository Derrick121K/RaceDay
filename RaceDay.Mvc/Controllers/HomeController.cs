using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() =>
        View("~/Views/Shared/ApiUnavailable.cshtml");
}

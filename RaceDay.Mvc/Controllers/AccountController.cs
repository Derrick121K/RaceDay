using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class AccountController : Controller
{
    private readonly RaceDayApiClient _api;

    public AccountController(RaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.LoginAsync(new LoginRequest
        {
            Email = model.Email.Trim(),
            Password = model.Password
        }, cancellationToken);

        if (result.IsSuccess)
            return RedirectToAction("Index", "Dashboard");

        ModelState.AddModelError(string.Empty, result.ErrorMessage ??
            "Sign-in failed. Please try again.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.RegisterParticipantAsync(new RegisterRequest
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            Role = "PARTICIPANT",
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim()
        }, cancellationToken);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Your participant account is ready. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ??
            "Registration failed. Please try again.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await _api.LogoutAsync(cancellationToken);
        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess
                ? "You have been signed out."
                : "This browser session was cleared, but the API could not confirm sign-out. " +
                  (result.ErrorMessage ?? "Please try again when the API is available.");

        return RedirectToAction(nameof(Login));
    }
}

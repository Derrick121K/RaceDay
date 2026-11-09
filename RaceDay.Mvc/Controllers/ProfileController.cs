using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class ProfileController : ProtectedPageController
{
    public ProfileController(RaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var failure = await RequireProfileAsync(cancellationToken);
        return failure ?? View(CurrentProfile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        var failure = await RequireProfileAsync(cancellationToken);
        if (failure is not null)
            return failure;

        if (image is null || image.Length == 0)
        {
            TempData["ErrorMessage"] = "Choose a profile image first.";
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadImageAsync(
            "api/users/me/profile-image", image, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "The profile image could not be uploaded.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Profile image updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Image(CancellationToken cancellationToken)
    {
        var failure = await RequireProfileAsync(cancellationToken);
        if (failure is not null)
            return failure;

        if (string.IsNullOrWhiteSpace(CurrentProfile.ProfileImagePath))
            return NotFound();

        var result = await Api.GetImageAsync(
            "api/users/me/profile-image", cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return NotFound();

        return File(result.Value.Bytes, result.Value.ContentType);
    }
}
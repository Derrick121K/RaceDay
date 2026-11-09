using RaceDay.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Handles account registration, login and logout for RaceDay.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    // ASP.NET Core provides the database context and password hasher
    // when it creates this controller for a request.
    public AuthController(
        RaceDayDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    // Tell Swagger which responses registration can return.
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Public registration must never grant organiser privileges.
        // Organiser accounts need to be provisioned through a trusted process.
        if (!string.Equals(
                request.Role.Trim(),
                "PARTICIPANT",
                StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // Store email addresses in a consistent format so that
        // the same email cannot be registered using different casing.
        var email = request.Email.Trim().ToLowerInvariant();

        // Check whether an account already uses this email address.
        var emailAlreadyExists = await _dbContext.Users
            .AnyAsync(user => user.Email == email);

        if (emailAlreadyExists)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        // Build the user record from the validated registration details.
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = "PARTICIPANT",

            // Phone numbers are optional, so save null when none is supplied.
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim(),

            CreatedAt = DateTime.Now
        };

        // Convert the supplied password into a secure hash.
        // The original password is never saved in the User table.
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        // Save the new account to SQL Server.
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // 201 Created means the account was registered successfully.
        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Account registered successfully."
        });
    }

    // Document successful login and the possible validation failures.
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request)
    {
        // Use the same email format as registration.
        var email = request.Email.Trim().ToLowerInvariant();

        // Find the account associated with this email address.
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(account => account.Email == email);

        // Use the same message for an unknown email and a wrong password.
        // This avoids revealing whether an email has an account.
        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Email or password is incorrect."
            });
        }

        // Compare the supplied password with the stored password hash.
        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Email or password is incorrect."
            });
        }

        // Refresh the stored hash if the hashing algorithm recommends it.
        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            await _dbContext.SaveChangesAsync();
        }

        // Start with a clean session so old identity values are removed.
        HttpContext.Session.Clear();

        // Remember who logged in and which role belongs to the account.
        // Later endpoints will use these values when checking permissions.
        HttpContext.Session.SetInt32("UserId", user.UserID);
        HttpContext.Session.SetString("Role", user.Role);

        // Return only safe account details, never the password hash.
        var response = new AuthResponse
        {
            UserId = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role
        };

        return Ok(response);
    }



    /// <summary>
    /// Returns the details of the user in the current session.
    /// </summary>
    [HttpGet("me")]
    [HttpGet("~/api/users/me")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser()
    {
        // Read the user ID from the server-side session.
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId is null)
        {
            return Unauthorized(new
            {
                message = "Please log in to access this resource."
            });
        }

        // Find that user's details in the database.
        var user = await _dbContext.Users
            .Where(account => account.UserID == userId.Value)
            .Select(account => new UserProfileResponse
            {
                UserId = account.UserID,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                ProfileImagePath = account.ProfileImagePath,
                Role = account.Role,
                CreatedAt = account.CreatedAt
            })
            .SingleOrDefaultAsync();

        // Reject a session if its user account no longer exists.
        if (user is null)
        {
            HttpContext.Session.Clear();

            return Unauthorized(new
            {
                message = "The session is no longer valid."
            });
        }

        return Ok(user);
    }

    /// <summary>
    /// Updates editable fields on the current user's profile.
    /// </summary>
    [HttpPut("me")]
    [HttpPut("~/api/users/me")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateCurrentUser(
        UserProfileUpdateRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId is null)
        {
            return Unauthorized(new
            {
                message = "Please log in to access this resource."
            });
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(account => account.UserID == userId.Value);

        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new
            {
                message = "The session is no longer valid."
            });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        await _dbContext.SaveChangesAsync();

        return Ok(new UserProfileResponse
        {
            UserId = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImagePath = user.ProfileImagePath,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }


    [HttpPost("~/api/users/me/profile-image")]
    [SessionAuthorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile file,
        [FromServices] IBlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return Unauthorized();

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user is null)
            return Unauthorized();

        try
        {
            user.ProfileImagePath = await blobStorage.UploadAsync(
                file, "profile-images", cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is not configured." });
        }

        return Ok(new ImageUploadResponse { ImagePath = user.ProfileImagePath! });
    }

    [HttpGet("~/api/users/me/profile-image")]
    [SessionAuthorize]
    public async Task<IActionResult> GetProfileImage(
        [FromServices] IBlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return Unauthorized();

        var imagePath = await _dbContext.Users
            .Where(user => user.UserID == userId.Value)
            .Select(user => user.ProfileImagePath)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(imagePath))
            return NotFound();

        try
        {
            var image = await blobStorage.DownloadAsync(imagePath, cancellationToken);
            if (image is null)
                return NotFound();

            return File(image.Content, image.ContentType);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
    // Logout clears the session and returns no response body.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Remove the stored user ID and role from the session.
        HttpContext.Session.Clear();

        return NoContent();
    }
}

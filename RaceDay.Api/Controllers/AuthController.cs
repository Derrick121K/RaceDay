
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
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

    /// <summary>
    /// Registers a new RaceDay account.
    /// The password is hashed before the account is saved.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
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
            Role = request.Role.Trim().ToUpperInvariant(),

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

    /// <summary>
    /// Checks a user's credentials and starts a session when valid.
    /// </summary>
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
    /// Ends the current session.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Remove the stored user ID and role from the session.
        HttpContext.Session.Clear();

        return NoContent();
    }
}

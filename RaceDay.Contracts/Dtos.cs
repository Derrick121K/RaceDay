using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

/// <summary>
/// Holds the details a person provides when creating a RaceDay account.
/// This class carries information to the API; it is not a database table.
/// </summary>
public sealed class RegisterRequest
{
    // A name is required when someone creates an account.
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    // We also need the person's surname.
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    // The email will identify the account during login.
    // We will also enforce email uniqueness in the database.
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    // The API must hash this password before storing the account.
    // Never save the original password as plain text.
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;

    // RaceDay supports Organisers and Participants.
    // This checks the submitted value; it does not grant permissions.
    [Required(ErrorMessage = "Please select a user role.")]
    [RegularExpression(
        "^(ORGANISER|PARTICIPANT)$",
        ErrorMessage = "Role must be ORGANISER or PARTICIPANT.")]
    public string Role { get; set; } = string.Empty;

    // A phone number can be supplied, but it is optional.
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Carries the email and password a registered user submits when
/// trying to sign in to RaceDay.
/// </summary>
public sealed class LoginRequest
{
    // The API uses the email to find the registered account.
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // The API will verify this against the stored password hash.
    // The password must never be stored as plain text.
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Contains safe user information returned after successful login.
/// Passwords and password hashes must never be included in this response.
/// </summary>
public sealed class AuthResponse
{
    // Identifies the account without exposing its password.
    public int UserId { get; set; }

    // These details help the application identify the signed-in user.
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Indicates whether the user is an ORGANISER or PARTICIPANT.
    // The API must enforce permissions separately.
    public string Role { get; set; } = string.Empty;
}



/// <summary>
/// Holds the details an organiser supplies when creating or updating an event.
/// </summary>
public sealed class EventRequest
{
    [Required]
    [StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal Distance { get; set; }

    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }
}

/// <summary>
/// Holds the information needed to create or update an event category.
/// </summary>
public sealed class CategoryRequest
{
    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinimumAge { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaximumAge { get; set; }
}

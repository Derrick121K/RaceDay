using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

/// <summary>
/// Represents a user account stored in the RaceDay database.
/// An account can belong to an Organiser or a Participant.
/// </summary>
public class User
{
    // The primary key uniquely identifies each user.
    [Key]
    public int UserID { get; set; }

    // Every account must have a first name.
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    // Every account must also have a surname.
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    // The email identifies the account during login.
    // We will configure the database to prevent duplicate emails.
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    // Store a secure password hash, never the original password.
    // The authentication service will create and verify this hash.
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    // Determines which actions the user may perform.
    // The API must check permissions before allowing protected actions.
    [Required]
    [RegularExpression("^(ORGANISER|PARTICIPANT)$")]
    public string Role { get; set; } = string.Empty;

    // A phone number is optional.
    public string? PhoneNumber { get; set; }

    // Records when the account was created.
    // We will set this value when a new account is saved.
    public DateTime CreatedAt { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

/// <summary>
/// Holds the information a person sends when registering for RaceDay.
/// This is a DTO (Data Transfer Object), which means it carries data
/// between the client application and our API.
/// It is not the database table itself.
/// </summary>
public sealed class RegisterRequest
{
    // A first name is required, and we limit its length to 50 characters.
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = string.Empty;


    // The surname is also required so that we can identify the user properly.
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = string.Empty;


    // We use the email address to identify the account during login.
    // The database will also need to prevent two accounts using the same email.
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;


    // The person enters a password when creating the account.
    // We must hash it in the API before saving anything to the database.
    // Never store the original password as plain text.
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;


    // RaceDay has two roles: ORGANISER and PARTICIPANT.
    // This validation checks that the submitted value matches one of them.
    // The registration logic must still enforce the rules for who may
    // register as an Organiser or Participant.
    [Required(ErrorMessage = "Please select a user role.")]
    [RegularExpression(
        "^(ORGANISER|PARTICIPANT)$",
        ErrorMessage = "Role must be ORGANISER or PARTICIPANT.")]
    public string Role { get; set; } = string.Empty;


    // A phone number is useful for contact, but it is optional.
    // The question mark means this property is allowed to be null.
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }
}
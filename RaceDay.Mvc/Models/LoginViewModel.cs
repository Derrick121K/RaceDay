using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

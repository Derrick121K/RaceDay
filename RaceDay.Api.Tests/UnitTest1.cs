
using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void RegisterRequest_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange: create a registration request with an invalid email.
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "Participant",
            Email = "not-an-email",
            Password = "RaceDayTest2026!",
            Role = "PARTICIPANT"
        };

        // Act: ask .NET to check the validation rules on the request.
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        var isValid = Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true);

        // Assert: the request must fail and report an email validation error.
        Assert.False(isValid);
        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(RegisterRequest.Email)));
    }
}

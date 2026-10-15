
using System.Net;
using System.Net.Http.Json;
using Xunit;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class AuthControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(RaceDayWebApplicationFactory factory)
    {
        // Create an HTTP client connected to our test API.
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange: prepare a request with invalid registration details.
        var request = new
        {
            firstName = "",
            lastName = "Participant",
            email = "not-an-email",
            password = "short",
            role = "PARTICIPANT",
            phoneNumber = (string?)null
        };

        // Act: send the request through the actual API pipeline.
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert: the API must reject the invalid request.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }



    [Fact]
    public async Task Register_WithValidDetails_ReturnsCreated()
    {
        // Arrange: use a unique email so the test can run repeatedly.
        var request = new
        {
            firstName = "Integration",
            lastName = "Participant",
            email = $"participant-{Guid.NewGuid():N}@example.com",
            password = "RaceDayTest2026!",
            role = "PARTICIPANT",
            phoneNumber = (string?)null
        };

        // Act: send the registration request to the API.
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert: a successful registration should return HTTP 201.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }


    [Fact]
    public async Task Register_TwiceWithSameEmail_ReturnsConflictOnSecondRequest()
    {
        // Arrange: create one registration request with a unique email.
        var request = new
        {
            firstName = "Duplicate",
            lastName = "Test",
            email = $"duplicate-{Guid.NewGuid():N}@example.com",
            password = "RaceDayTest2026!",
            role = "PARTICIPANT",
            phoneNumber = (string?)null
        };

        // Act: register the account for the first time.
        var firstResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Act again: submit the exact same email.
        var secondResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert: the first request succeeds.
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Assert: the duplicate request is rejected.
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }


    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        // Arrange: register a user for this test.
        var email = $"login-test-{Guid.NewGuid():N}@example.com";

        var registration = new
        {
            firstName = "Login",
            lastName = "Test",
            email = email,
            password = "RaceDayTest2026!",
            role = "PARTICIPANT",
            phoneNumber = (string?)null
        };

        var registrationResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            registration);

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        // Act: attempt to log in with the wrong password.
        var loginRequest = new
        {
            email = email,
            password = "WrongPassword2026!"
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        // Assert: the API must reject the incorrect password.
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }


    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsUserDetails()
    {
        // Arrange: register a new participant with a unique email.
        var email = $"valid-login-{Guid.NewGuid():N}@example.com";
        var password = "RaceDayTest2026!";

        var registration = new
        {
            firstName = "Successful",
            lastName = "Login",
            email = email,
            password = password,
            role = "PARTICIPANT",
            phoneNumber = (string?)null
        };

        var registrationResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            registration);

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        // Act: log in with the correct email and password.
        var loginRequest = new
        {
            email = email,
            password = password
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        // Assert: login succeeds and returns the participant's details.
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var userDetails =
            await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(userDetails);
        Assert.Equal(email, userDetails.Email);
        Assert.Equal("Successful", userDetails.FirstName);
        Assert.Equal("PARTICIPANT", userDetails.Role);
    }


    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        // Act: send a logout request to the API.
        var response = await _client.PostAsync(
            "/api/auth/logout",
            content: null);

        // Assert: logout should return HTTP 204.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }


}

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Mvc.Tests;

public class AuthenticationAndCookieTests : IClassFixture<MvcApplicationFactory>
{
    private readonly MvcApplicationFactory _factory;

    public AuthenticationAndCookieTests(MvcApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginSuccessForwardsApiCookieAndUsesItOnProtectedRequests()
    {
        using var client = CreateClient();
        var response = await SignInAsync(client, "participant-alex@example.com");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("Path=/", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=None", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expires=Wed, 01 Jan 2031", setCookie, StringComparison.Ordinal);
        var sessionPair = setCookie.Split(';', 2)[0];

        var dashboard = await client.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("Alex", await dashboard.Content.ReadAsStringAsync());
        Assert.Contains(_factory.ApiState.Requests, request =>
            request.Path == "/api/users/me" && request.Cookie == sessionPair);
    }

    [Fact]
    public async Task LoginFailureShowsInvalidCredentialMessage()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
        [
            new("Email", "participant-alex@example.com"),
            new("Password", "wrong-password"),
            new("__RequestVerificationToken", token)
        ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Email or password is incorrect", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FormPostWithoutAntiforgeryTokenIsRejected()
    {
        using var client = CreateClient();
        var requestCount = _factory.ApiState.Requests.Count;

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
        [
            new("Email", "participant-alex@example.com"),
            new("Password", "RaceDayMvcTest2026!")
        ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(requestCount, _factory.ApiState.Requests.Count);
    }

    [Fact]
    public async Task RegistrationFormCreatesParticipantOnly()
    {
        using var client = CreateClient();
        var form = await client.GetAsync("/Account/Register");
        var html = await form.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Organiser registration", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"Role\"", html, StringComparison.OrdinalIgnoreCase);

        var token = await GetAntiforgeryTokenAsync(client, "/Account/Register");
        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(
        [
            new("FirstName", "Alex"),
            new("LastName", "Runner"),
            new("Email", "participant-new@example.com"),
            new("Password", "RaceDayMvcTest2026!"),
            new("__RequestVerificationToken", token)
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("PARTICIPANT", Assert.Single(_factory.ApiState.RegistrationRoles));
    }

    [Fact]
    public async Task LogoutExpiresBrowserCookieAndDoesNotForwardItAgain()
    {
        using var client = CreateClient();
        await SignInAsync(client, "participant-alex@example.com");
        var token = await GetAntiforgeryTokenAsync(client, "/Dashboard");

        var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(
        [new("__RequestVerificationToken", token)]));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var clearCookie = Assert.Single(logout.Headers.GetValues("Set-Cookie")
            .Where(value => value.StartsWith(".RaceDay.Session=", StringComparison.Ordinal)));
        Assert.Contains(".RaceDay.Session=", clearCookie, StringComparison.Ordinal);
        Assert.Contains("Max-Age=0", clearCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/", clearCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpOnly", clearCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", clearCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=Lax", clearCookie, StringComparison.OrdinalIgnoreCase);

        var protectedPage = await client.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Equal("/Account/Login", protectedPage.Headers.Location!.OriginalString);
        Assert.Contains(_factory.ApiState.Requests, request =>
            request.Path == "/api/auth/logout" &&
            request.Cookie?.StartsWith(".RaceDay.Session=", StringComparison.Ordinal) == true);
        Assert.Contains(_factory.ApiState.Requests, request =>
            request.Path == "/api/users/me" && request.Cookie is null);
    }

    [Fact]
    public async Task UnauthenticatedProtectedPageRedirectsToLogin()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task NavigationShowsRoleSpecificDashboardAndSessionsStayIsolated()
    {
        using var participantClient = CreateClient();
        using var organiserClient = CreateClient();
        await SignInAsync(participantClient, "participant-bob@example.com");
        await SignInAsync(organiserClient, "organiser-olivia@example.com");

        var participantPage = await participantClient.GetAsync("/Dashboard");
        var organiserPage = await organiserClient.GetAsync("/Dashboard");
        var participantHtml = await participantPage.Content.ReadAsStringAsync();
        var organiserHtml = await organiserPage.Content.ReadAsStringAsync();

        Assert.Contains("Participant dashboard", participantHtml);
        Assert.Contains("PARTICIPANT", participantHtml);
        Assert.Contains("Organiser dashboard", organiserHtml);
        Assert.Contains("ORGANISER", organiserHtml);
        Assert.Contains("Bob", participantHtml);
        Assert.Contains("Olivia", organiserHtml);

        var profileRequests = _factory.ApiState.Requests
            .Where(request => request.Path == "/api/users/me")
            .Select(request => request.Cookie)
            .Where(cookie => cookie is not null)
            .ToArray();
        Assert.True(profileRequests.Length >= 2);
        Assert.NotEqual(profileRequests[^1], profileRequests[^2]);
    }

    [Fact]
    public async Task ApiConnectionFailureShowsServiceUnavailableMessage()
    {
        _factory.ApiState.FailRequests = true;
        using var client = CreateClient();

        var response = await client.GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("RaceDay API could not be reached", await response.Content.ReadAsStringAsync());
        _factory.ApiState.FailRequests = false;
    }

    [Fact]
    public async Task ExpiredSessionRedirectsToLoginWithExplanation()
    {
        using var client = CreateClient();
        await SignInAsync(client, "participant-alex@example.com");
        _factory.ApiState.RevokeSessions = true;

        var expired = await client.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
        var login = await client.GetAsync(expired.Headers.Location);

        Assert.Contains("Your session has expired", await login.Content.ReadAsStringAsync());
        _factory.ApiState.RevokeSessions = false;
    }

    [Fact]
    public async Task ForbiddenApiResponseShowsForbiddenPage()
    {
        using var client = CreateClient();
        await SignInAsync(client, "participant-alex@example.com");
        _factory.ApiState.ProfileStatusOverride = HttpStatusCode.Forbidden;

        var response = await client.GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("not available to your account", await response.Content.ReadAsStringAsync());
        _factory.ApiState.ProfileStatusOverride = null;
    }

    private HttpClient CreateClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private static async Task<HttpResponseMessage> SignInAsync(
        HttpClient client,
        string email)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
        [
            new("Email", email),
            new("Password", "RaceDayMvcTest2026!"),
            new("__RequestVerificationToken", token)
        ]));
    }

    private static async Task<string> GetAntiforgeryTokenAsync(
        HttpClient client,
        string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\\\"__RequestVerificationToken\\\"[^>]*value=\\\"([^\\\"]+)\\\"",
            RegexOptions.CultureInvariant);
        Assert.True(match.Success, "Antiforgery token was not rendered in the form.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}

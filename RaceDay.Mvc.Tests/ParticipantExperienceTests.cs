using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Contracts;

namespace RaceDay.Mvc.Tests;

public sealed class ParticipantExperienceTests : IClassFixture<MvcApplicationFactory>
{
    private readonly MvcApplicationFactory _factory;
    public ParticipantExperienceTests(MvcApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ParticipantPagesRequireParticipantSession()
    {
        using var anonymous = CreateClient();
        var redirect = await anonymous.GetAsync("/participant/events");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal("/Account/Login", redirect.Headers.Location!.OriginalString);

        using var organiser = CreateClient();
        await SignInAsync(organiser, "organiser-participant-pages@example.com");
        var denied = await organiser.GetAsync("/participant/results");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task ParticipantDashboardShowsSafePersonalSummaryAndNavigation()
    {
        _factory.ApiState.ParticipantEnrolments = [new EventEnrolmentResponse
        {
            EnrolmentID = 12, EventID = 1, CategoryID = 2, EnrolmentDate = new DateTime(2026, 4, 1), Status = "ACTIVE"
        }];
        _factory.ApiState.ParticipantResults = [new ParticipantResultResponse
        {
            ResultId = 21, EventName = "Test race", EventDate = new DateTime(2027, 5, 1),
            CategoryName = "Open", FinishTime = TimeSpan.FromHours(1), FinishingPosition = 2
        }];
        using var client = CreateClient();
        await SignInAsync(client, "participant-dashboard@example.com");
        var response = await client.GetAsync("/participant");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Welcome, Alex", html);
        Assert.Contains("ACTIVE", html);
        Assert.Contains("Test race", html);
        Assert.Contains(("GET", "/api/enrolments/me"), _factory.ApiState.ApiCalls);
        Assert.Contains(("GET", "/api/results/me"), _factory.ApiState.ApiCalls);
        var resultCookie = _factory.ApiState.Requests.Last(request => request.Path == "/api/results/me").Cookie;
        Assert.NotNull(resultCookie);

        var results = await client.GetAsync("/participant/results");
        var resultHtml = await results.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
        Assert.Contains("Test race", resultHtml);
        Assert.Contains("Open", resultHtml);
        Assert.Contains("01:00:00", resultHtml);
    }

    [Fact]
    public async Task ParticipantCanDiscoverEventsAndViewCategories()
    {
        using var client = CreateClient();
        await SignInAsync(client, "participant-discovery@example.com");
        var events = await client.GetAsync("/participant/events?search=Cape");
        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.Contains("Test race", await events.Content.ReadAsStringAsync());
        Assert.Contains(("GET", "/api/events"), _factory.ApiState.ApiCalls);

        var detail = await client.GetAsync("/participant/events/1");
        var html = await detail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("Open", html);
        Assert.Contains("Cape Town", html);
        Assert.Contains(("GET", "/api/events/1/categories"), _factory.ApiState.ApiCalls);
    }

    [Fact]
    public async Task ParticipantCanEnrolAndDuplicateIsRejectedClearly()
    {
        _factory.ApiState.ParticipantEnrolments = [];
        _factory.ApiState.CreatedEnrolments.Clear();
        _factory.ApiState.EnrolmentExists = false;
        using var client = CreateClient();
        await SignInAsync(client, "participant-enrol@example.com");
        var token = await TokenAsync(client, "/participant/events/1/enrol");
        var saved = await client.PostAsync("/participant/events/1/enrol", Form(token, ("CategoryId", "2")));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Equal(2, _factory.ApiState.CreatedEnrolments.Last().CategoryId);

        _factory.ApiState.EnrolmentExists = true;
        token = await TokenAsync(client, "/participant/events/1/enrol");
        var duplicate = await client.PostAsync("/participant/events/1/enrol", Form(token, ("CategoryId", "2")));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("already enrolled", await duplicate.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParticipantSeesOwnEnrolmentsAndResultsAndEmptyStates()
    {
        _factory.ApiState.ParticipantEnrolments = [];
        _factory.ApiState.ParticipantResults = [];
        using var client = CreateClient();
        await SignInAsync(client, "participant-empty@example.com");
        var enrolments = await client.GetAsync("/participant/enrolments");
        Assert.Equal(HttpStatusCode.OK, enrolments.StatusCode);
        Assert.Contains("have not enrolled", await enrolments.Content.ReadAsStringAsync());
        var results = await client.GetAsync("/participant/results");
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
        Assert.Contains("do not have any recorded results", await results.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ParticipantApiFailureShowsHelpfulError()
    {
        using var client = CreateClient();
        await SignInAsync(client, "participant-api-failure@example.com");
        _factory.ApiState.ApiStatusOverrides["/api/events"] = HttpStatusCode.InternalServerError;
        var response = await client.GetAsync("/participant/events");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        _factory.ApiState.ApiStatusOverrides.TryRemove("/api/events", out _);
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        client.Timeout = TimeSpan.FromSeconds(10);
        return client;
    }

    private static FormUrlEncodedContent Form(string token, params (string Name, string Value)[] values) =>
        new(values.Select(pair => new KeyValuePair<string, string>(pair.Name, pair.Value))
            .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)));

    private static async Task SignInAsync(HttpClient client, string email)
    {
        var token = await TokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(token,
            ("Email", email), ("Password", "RaceDayMvcTest2026!")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\\\"__RequestVerificationToken\\\"[^>]*value=\\\"([^\\\"]+)\\\"");
        Assert.True(match.Success, $"Antiforgery token missing at {path} (HTTP {(int)response.StatusCode}).");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}

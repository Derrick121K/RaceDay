using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Mvc.Tests;

public sealed class OrganiserManagementTests : IClassFixture<MvcApplicationFactory>
{
    private readonly MvcApplicationFactory _factory;
    public OrganiserManagementTests(MvcApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task OrganiserPagesRequireLoginAndRejectParticipants()
    {
        using var anonymous = CreateClient();
        var redirect = await anonymous.GetAsync("/organiser/events");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal("/Account/Login", redirect.Headers.Location!.OriginalString);

        using var participant = CreateClient();
        await SignInAsync(participant, "participant-organiser-test@example.com");
        var denied = await participant.GetAsync("/organiser/events");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task OrganiserCanViewEventsAndEventDetailsThroughApi()
    {
        using var client = CreateClient();
        await SignInAsync(client, "organiser-events-test@example.com");
        var events = await client.GetAsync("/organiser/events");
        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.Contains("Test race", await events.Content.ReadAsStringAsync());
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("GET", "/api/events"));

        var details = await client.GetAsync("/organiser/events/1");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("Categories", await details.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OrganiserCanCreateAndEditEventsWithApiEventTypes()
    {
        using var client = CreateClient();
        await SignInAsync(client, "organiser-event-edit@example.com");
        var form = await client.GetAsync("/organiser/events/create");
        var formHtml = await form.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, form.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("GET", "/api/event-types"));
        Assert.Contains("Road", formHtml);
        var token = await TokenAsync(client, "/organiser/events/create");
        var created = await client.PostAsync("/organiser/events/create", Form(token,
            ("EventName", "Autumn run"), ("Description", "Ten kilometre road run"),
            ("EventDate", "2027-05-01"), ("Location", "Cape Town"), ("Distance", "10"), ("EventTypeId", "1")));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("POST", "/api/events"));

        token = await TokenAsync(client, "/organiser/events/1/edit");
        var edited = await client.PostAsync("/organiser/events/1/edit", Form(token,
            ("EventName", "Updated run"), ("Description", "Updated details"),
            ("EventDate", "2027-06-02"), ("Location", "Durban"), ("Distance", "21.1"),
            ("EventTypeId", "1"), ("__Invariant", "Distance")));
        Assert.True(edited.StatusCode == HttpStatusCode.Redirect, await edited.Content.ReadAsStringAsync());
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("PUT", "/api/events/1"));
    }

    [Fact]
    public async Task OrganiserCanManageCategoriesAndEnrolments()
    {
        using var client = CreateClient();
        await SignInAsync(client, "organiser-category-test@example.com");
        var categories = await client.GetAsync("/organiser/events/1/categories");
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
        Assert.Contains("Open", await categories.Content.ReadAsStringAsync());
        var token = await TokenAsync(client, "/organiser/events/1/categories/create");
        var created = await client.PostAsync("/organiser/events/1/categories/create", Form(token,
            ("CategoryName", "Veterans"), ("Description", "Veteran runners"), ("MinimumAge", "40"), ("MaximumAge", "99")));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("POST", "/api/events/1/categories"));
        token = await TokenAsync(client, "/organiser/events/1/categories/2/edit");
        var updated = await client.PostAsync("/organiser/events/1/categories/2/edit", Form(token,
            ("CategoryName", "Open Plus"), ("Description", "Updated category"), ("MinimumAge", "18"), ("MaximumAge", "99")));
        Assert.Equal(HttpStatusCode.Redirect, updated.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("PUT", "/api/categories/2"));
        token = await TokenAsync(client, "/organiser/events/1/categories");
        var deleted = await client.PostAsync("/organiser/events/1/categories/2/delete", Form(token));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("DELETE", "/api/categories/2"));

        var enrolments = await client.GetAsync("/organiser/events/1/enrolments");
        Assert.Equal(HttpStatusCode.OK, enrolments.StatusCode);
        Assert.Contains("Participant #9", await enrolments.Content.ReadAsStringAsync());
        token = await TokenAsync(client, "/organiser/events/1/enrolments");
        var status = await client.PostAsync("/organiser/events/1/enrolments/7/status", Form(token, ("status", "COMPLETED")));
        Assert.Equal(HttpStatusCode.Redirect, status.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("PUT", "/api/enrolments/7/status"));
    }

    [Fact]
    public async Task OrganiserCanReviewAndRecordResultsForEventParticipants()
    {
        using var client = CreateClient();
        await SignInAsync(client, "organiser-results-test@example.com");
        var results = await client.GetAsync("/organiser/events/1/results");
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
        Assert.Contains("Not recorded", await results.Content.ReadAsStringAsync());
        var token = await TokenAsync(client, "/organiser/events/1/results/7/edit");
        var saved = await client.PostAsync("/organiser/events/1/results/7/edit", Form(token,
            ("FinishTime", "01:22:14"), ("FinishingPosition", "1")));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("POST", "/api/enrolments/7/result"));

        token = await TokenAsync(client, "/organiser/events/1/results/7/edit");
        var corrected = await client.PostAsync("/organiser/events/1/results/7/edit", Form(token,
            ("FinishTime", "01:20:00"), ("FinishingPosition", "2")));
        Assert.Equal(HttpStatusCode.Redirect, corrected.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("PUT", "/api/enrolments/7/result"));

        token = await TokenAsync(client, "/organiser/events/1/results");
        var removed = await client.PostAsync("/organiser/events/1/results/7/delete", Form(token));
        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.Contains(_factory.ApiState.ApiCalls, call => call == ("DELETE", "/api/enrolments/7/result"));

        token = await TokenAsync(client, "/organiser/events/1/results/7/edit");
        var invalid = await client.PostAsync("/organiser/events/1/results/7/edit", Form(token,
            ("FinishTime", "not-a-time"), ("FinishingPosition", "0")));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("Enter a time", await invalid.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EventOwnershipIsCheckedBeforeShowingManagementActions()
    {
        using var client = CreateClient();
        await SignInAsync(client, "organiser-owner-test@example.com");
        var denied = await client.GetAsync("/organiser/events/99/categories");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task OrganiserApiFailureShowsGatewayError()
    {
        using var client = CreateClient();
        await SignInAsync(client, "organiser-api-failure@example.com");
        _factory.ApiState.ApiStatusOverrides["/api/events"] = HttpStatusCode.InternalServerError;
        var response = await client.GetAsync("/organiser/events");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("request was rejected", await response.Content.ReadAsStringAsync());
        _factory.ApiState.ApiStatusOverrides.TryRemove("/api/events", out _);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

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

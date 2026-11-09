using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Tests;

public sealed class FakeApiState
{
    public ConcurrentDictionary<string, UserProfileResponse> Sessions { get; } = new();
    public ConcurrentQueue<(string Path, string? Cookie)> Requests { get; } = new();
    public ConcurrentQueue<(string Method, string Path)> ApiCalls { get; } = new();
    public ConcurrentQueue<string> RegistrationRoles { get; } = new();
    public ConcurrentDictionary<string, HttpStatusCode> ApiStatusOverrides { get; } = new();
    public bool FailRequests { get; set; }
    public HttpStatusCode? ProfileStatusOverride { get; set; }
    public bool RevokeSessions { get; set; }
    public bool ResultIsRecorded { get; set; }
    public bool EnrolmentExists { get; set; }
    public IReadOnlyList<EventEnrolmentResponse> ParticipantEnrolments { get; set; } = [];
    public IReadOnlyList<ParticipantResultResponse> ParticipantResults { get; set; } = [];
    public ConcurrentQueue<EnrolmentRequest> CreatedEnrolments { get; } = new();
}

public sealed class MvcApplicationFactory : WebApplicationFactory<Program>
{
    public FakeApiState ApiState { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            // TestServer has no Windows Event Log target; preserve diagnostics on console.
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();
            services.RemoveAll<FakeApiState>();
            services.AddSingleton(ApiState);
            services.AddHttpClient<RaceDayApiClient>()
                .ConfigurePrimaryHttpMessageHandler(provider =>
                    new FakeRaceDayApiHandler(provider.GetRequiredService<FakeApiState>()));
        });
    }
}

internal sealed class FakeRaceDayApiHandler : HttpMessageHandler
{
    private readonly FakeApiState _state;

    public FakeRaceDayApiHandler(FakeApiState state)
    {
        _state = state;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_state.FailRequests)
            throw new HttpRequestException("Test API is offline.");

        var path = request.RequestUri!.AbsolutePath;
        var cookie = request.Headers.TryGetValues("Cookie", out var values)
            ? values.SingleOrDefault()
            : null;
        _state.Requests.Enqueue((path, cookie));
        _state.ApiCalls.Enqueue((request.Method.Method, path));

        if (_state.ApiStatusOverrides.TryGetValue(path, out var statusOverride))
            return ErrorResponse(statusOverride);

        if (path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
            return await LoginAsync(request, cancellationToken);

        if (path.Equals("/api/auth/register", StringComparison.OrdinalIgnoreCase))
        {
            var body = await request.Content!.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);
            _state.RegistrationRoles.Enqueue(body.GetProperty("role").GetString() ?? "");
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new { message = "Account registered." })
            };
        }

        if (path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
            return new HttpResponseMessage(HttpStatusCode.NoContent);

        if (path.Equals("/api/users/me", StringComparison.OrdinalIgnoreCase))
        {
            if (_state.ProfileStatusOverride is { } overrideStatus)
                return ErrorResponse(overrideStatus);

            if (cookie is null || !cookie.StartsWith(".RaceDay.Session=", StringComparison.Ordinal))
                return ErrorResponse(HttpStatusCode.Unauthorized);

            var sessionId = cookie[".RaceDay.Session=".Length..];
            if (_state.RevokeSessions || !_state.Sessions.TryGetValue(sessionId, out var profile))
                return ErrorResponse(HttpStatusCode.Unauthorized);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(profile)
            };
        }

        var currentProfile = GetProfile(cookie);
        if (path == "/api/enrolments/me" && request.Method == HttpMethod.Get)
            return currentProfile?.Role == "PARTICIPANT"
                ? Json(_state.ParticipantEnrolments)
                : ErrorResponse(currentProfile is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        if (path == "/api/results/me" && request.Method == HttpMethod.Get)
            return currentProfile?.Role == "PARTICIPANT"
                ? Json(_state.ParticipantResults)
                : ErrorResponse(currentProfile is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        if (path == "/api/enrolments" && request.Method == HttpMethod.Post)
        {
            if (currentProfile?.Role != "PARTICIPANT")
                return ErrorResponse(currentProfile is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
            var requestBody = await request.Content!.ReadFromJsonAsync<EnrolmentRequest>(cancellationToken: cancellationToken);
            _state.CreatedEnrolments.Enqueue(requestBody!);
            return _state.EnrolmentExists
                ? ErrorResponse(HttpStatusCode.Conflict, "You have already enrolled in this event.")
                : Json(new { enrolmentID = 8, eventID = requestBody!.EventId, categoryID = requestBody.CategoryId, status = "ACTIVE" }, HttpStatusCode.Created);
        }
        if (path == "/api/events" && request.Method == HttpMethod.Get)
            return Json(new[] { SampleEvent(currentProfile?.UserId ?? 1) });
        if (path.StartsWith("/api/events/", StringComparison.Ordinal) &&
            path.EndsWith("/categories", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            return Json(new[] { SampleCategory() });
        if (path == "/api/event-types" && request.Method == HttpMethod.Get)
            return Json(new[] { new EventTypeResponse { EventTypeID = 1, TypeName = "Road" } });
        if (path.StartsWith("/api/events/", StringComparison.Ordinal) &&
            path.EndsWith("/enrolments", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            return Json(new[] { SampleEnrolment() });
        if (path.StartsWith("/api/enrolments/", StringComparison.Ordinal) &&
            path.EndsWith("/result", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            return _state.ResultIsRecorded
                ? Json(new RaceResultResponse { ResultID = 5, EnrolmentID = 7, FinishTime = TimeSpan.FromHours(1), FinishingPosition = 1 })
                : ErrorResponse(HttpStatusCode.NotFound);
        if (path.StartsWith("/api/enrolments/", StringComparison.Ordinal) &&
            path.EndsWith("/result", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
        {
            _state.ResultIsRecorded = true;
            return Json(new RaceResultResponse { ResultID = 5, EnrolmentID = 7, FinishTime = TimeSpan.FromHours(1), FinishingPosition = 1 }, HttpStatusCode.Created);
        }
        if (path.StartsWith("/api/enrolments/", StringComparison.Ordinal) && path.EndsWith("/result", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            return Json(new RaceResultResponse { ResultID = 5, EnrolmentID = 7, FinishTime = TimeSpan.FromHours(1), FinishingPosition = 1 });
        if (path.StartsWith("/api/enrolments/", StringComparison.Ordinal) && path.EndsWith("/result", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
        {
            _state.ResultIsRecorded = false;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
        if (path.StartsWith("/api/enrolments/", StringComparison.Ordinal) && path.EndsWith("/status", StringComparison.Ordinal))
            return new HttpResponseMessage(HttpStatusCode.OK);
        if (path.StartsWith("/api/enrolments/", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
            return Json(new EnrolmentDetailResponse { EnrolmentId = 7, ParticipantId = 9, EventId = 1, EventName = "Test race", CategoryId = 2, CategoryName = "Open", Status = "ACTIVE" });
        if (path.StartsWith("/api/events/", StringComparison.Ordinal) && path.EndsWith("/categories", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            return Json(new CategoryResponse { CategoryID = 2, EventID = 1, CategoryName = "Open" }, HttpStatusCode.Created);
        if (path.StartsWith("/api/categories/", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            return Json(SampleCategory());
        if (path.StartsWith("/api/categories/", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        if (path == "/api/events" && request.Method == HttpMethod.Post)
            return Json(SampleEvent(currentProfile?.UserId ?? 1), HttpStatusCode.Created);
        if (path.StartsWith("/api/events/", StringComparison.Ordinal) && request.Method == HttpMethod.Put)
            return Json(SampleEvent(currentProfile?.UserId ?? 1));
        if (path.StartsWith("/api/events/", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
        {
            var eventId = int.TryParse(path.Split('/').Last(), out var parsedId) ? parsedId : 1;
            return Json(SampleEvent(eventId == 99 ? -123 : currentProfile?.UserId ?? 1));
        }

        return ErrorResponse(HttpStatusCode.NotFound);
    }

    private UserProfileResponse? GetProfile(string? cookie)
    {
        if (cookie is null || !cookie.StartsWith(".RaceDay.Session=", StringComparison.Ordinal)) return null;
        var sessionId = cookie[".RaceDay.Session=".Length..];
        return _state.Sessions.TryGetValue(sessionId, out var profile) ? profile : null;
    }

    private static EventResponse SampleEvent(int organiserId) => new()
    {
        EventID = 1, EventName = "Test race", Description = "A test event", EventDate = new DateTime(2027, 5, 1),
        Location = "Cape Town", Distance = 10, EventTypeID = 1, EventTypeName = "Road", OrganiserID = organiserId
    };

    private static CategoryResponse SampleCategory() => new()
    {
        CategoryID = 2, EventID = 1, CategoryName = "Open", Description = "Open category", MinimumAge = 18
    };

    private static EventEnrolmentResponse SampleEnrolment() => new()
    {
        EnrolmentID = 7, ParticipantID = 9, EventID = 1, CategoryID = 2,
        EnrolmentDate = new DateTime(2026, 4, 1), Status = "ACTIVE"
    };

    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = JsonContent.Create(value)
    };

    private async Task<HttpResponseMessage> LoginAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
        var email = body.GetProperty("email").GetString()!;
        var password = body.GetProperty("password").GetString();
        if (password != "RaceDayMvcTest2026!")
            return ErrorResponse(HttpStatusCode.Unauthorized, "Email or password is incorrect.");

        var isOrganiser = email.StartsWith("organiser-", StringComparison.OrdinalIgnoreCase);
        var profile = new UserProfileResponse
        {
            UserId = Math.Abs(email.GetHashCode()),
            FirstName = isOrganiser ? "Olivia" : email.Contains("bob", StringComparison.OrdinalIgnoreCase) ? "Bob" : "Alex",
            LastName = isOrganiser ? "Organiser" : "Runner",
            Email = email,
            Role = isOrganiser ? "ORGANISER" : "PARTICIPANT",
            CreatedAt = new DateTime(2026, 1, 1)
        };
        var sessionId = Guid.NewGuid().ToString("N");
        _state.Sessions[sessionId] = profile;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthResponse
            {
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                Role = profile.Role
            })
        };
        response.Headers.TryAddWithoutValidation(
            "Set-Cookie",
            $".RaceDay.Session={sessionId}; Path=/; HttpOnly; Secure; SameSite=None; Expires=Wed, 01 Jan 2031 00:00:00 GMT");
        return response;
    }

    private static HttpResponseMessage ErrorResponse(
        HttpStatusCode status,
        string? message = null) => new(status)
        {
            Content = JsonContent.Create(new
            {
                message = message ?? (status == HttpStatusCode.Forbidden
                    ? "You do not have permission."
                    : "The request was rejected.")
            })
        };
}

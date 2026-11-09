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
    public ConcurrentQueue<string> RegistrationRoles { get; } = new();
    public bool FailRequests { get; set; }
    public HttpStatusCode? ProfileStatusOverride { get; set; }
    public bool RevokeSessions { get; set; }
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

        return ErrorResponse(HttpStatusCode.NotFound);
    }

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

using Microsoft.Net.Http.Headers;
using System.Globalization;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Forwards only the API's session cookie to the API and relays API cookie
/// headers to the current browser response. No cookie jar is shared between users.
/// </summary>
public sealed class ApiSessionCookieHandler : DelegatingHandler
{
    public const string SessionCookieName = ".RaceDay.Session";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiSessionCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var isLogout = request.RequestUri?.AbsolutePath.Equals(
            "/api/auth/logout",
            StringComparison.OrdinalIgnoreCase) == true;

        if (context is not null &&
            context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionCookie) &&
            !string.IsNullOrEmpty(sessionCookie))
        {
            request.Headers.TryAddWithoutValidation(
                HeaderNames.Cookie,
                $"{SessionCookieName}={sessionCookie}");
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch when (isLogout && context is not null)
        {
            ExpireBrowserSessionCookie(context);
            throw;
        }

        if (context is not null)
        {
            var apiClearedSessionCookie = false;
            if (response.Headers.TryGetValues(HeaderNames.SetCookie, out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    context.Response.Headers.Append(HeaderNames.SetCookie, cookie);
                    apiClearedSessionCookie |= IsExpiredSessionCookie(cookie);
                }
            }

            // ASP.NET Core session may clear server-side state without emitting
            // an expired browser cookie. Add one if the API omitted it.
            if (isLogout && !apiClearedSessionCookie)
                ExpireBrowserSessionCookie(context);
        }

        return response;
    }

    private static void ExpireBrowserSessionCookie(HttpContext context)
    {
        var cookie = $"{SessionCookieName}=; Path=/; Max-Age=0; " +
                     "Expires=Thu, 01 Jan 1970 00:00:00 GMT; HttpOnly; SameSite=Lax";
        if (context.Request.IsHttps)
            cookie += "; Secure";

        context.Response.Headers.Append(HeaderNames.SetCookie, cookie);
    }

    private static bool IsExpiredSessionCookie(string cookie)
    {
        var attributes = cookie.Split(';');
        var name = attributes[0].Split('=', 2)[0].Trim();
        if (!name.Equals(SessionCookieName, StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var attribute in attributes.Skip(1).Select(value => value.Trim()))
        {
            if (attribute.Equals("Max-Age=0", StringComparison.OrdinalIgnoreCase))
                return true;

            if (attribute.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase) &&
                DateTimeOffset.TryParse(
                    attribute["Expires=".Length..],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var expiry) &&
                expiry <= DateTimeOffset.UtcNow)
            {
                return true;
            }
        }

        return false;
    }
}

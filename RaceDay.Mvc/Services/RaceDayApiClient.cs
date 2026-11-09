using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient
{
    private readonly HttpClient _httpClient;

    public RaceDayApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ApiCallResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, cancellationToken);

    public Task<ApiCallResult<object>> RegisterParticipantAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Post, "api/auth/register", request, cancellationToken);

    public Task<ApiCallResult<UserProfileResponse>> GetCurrentProfileAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<UserProfileResponse>(HttpMethod.Get, "api/users/me", null, cancellationToken);

    public Task<ApiCallResult<IReadOnlyList<EventResponse>>> GetEventsAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<EventResponse>>(
            HttpMethod.Get, "api/events", null, cancellationToken);

    public Task<ApiCallResult<EventResponse>> GetEventAsync(
        int eventId,
        CancellationToken cancellationToken = default) =>
        SendAsync<EventResponse>(
            HttpMethod.Get, $"api/events/{eventId}", null, cancellationToken);

    public Task<ApiCallResult<IReadOnlyList<EventTypeResponse>>> GetEventTypesAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<EventTypeResponse>>(
            HttpMethod.Get, "api/event-types", null, cancellationToken);

    public Task<ApiCallResult<EventResponse>> CreateEventAsync(
        EventRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<EventResponse>(HttpMethod.Post, "api/events", request, cancellationToken);

    public Task<ApiCallResult<EventResponse>> UpdateEventAsync(
        int eventId,
        EventRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<EventResponse>(HttpMethod.Put, $"api/events/{eventId}", request, cancellationToken);

    public Task<ApiCallResult<IReadOnlyList<CategoryResponse>>> GetEventCategoriesAsync(
        int eventId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<CategoryResponse>>(
            HttpMethod.Get, $"api/events/{eventId}/categories", null, cancellationToken);

    public Task<ApiCallResult<CategoryResponse>> CreateCategoryAsync(
        int eventId,
        CategoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CategoryResponse>(
            HttpMethod.Post, $"api/events/{eventId}/categories", request, cancellationToken);

    public Task<ApiCallResult<CategoryResponse>> UpdateCategoryAsync(
        int categoryId,
        CategoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CategoryResponse>(
            HttpMethod.Put, $"api/categories/{categoryId}", request, cancellationToken);

    public Task<ApiCallResult<object>> DeleteCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Delete, $"api/categories/{categoryId}", null, cancellationToken);

    public Task<ApiCallResult<IReadOnlyList<EventEnrolmentResponse>>> GetEventEnrolmentsAsync(
        int eventId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<EventEnrolmentResponse>>(
            HttpMethod.Get, $"api/events/{eventId}/enrolments", null, cancellationToken);

    public Task<ApiCallResult<EnrolmentDetailResponse>> GetEnrolmentAsync(
        int enrolmentId,
        CancellationToken cancellationToken = default) =>
        SendAsync<EnrolmentDetailResponse>(
            HttpMethod.Get, $"api/enrolments/{enrolmentId}", null, cancellationToken);

    public Task<ApiCallResult<object>> UpdateEnrolmentStatusAsync(
        int enrolmentId,
        EnrolmentStatusRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(
            HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", request, cancellationToken);

    public Task<ApiCallResult<RaceResultResponse>> GetEnrolmentResultAsync(
        int enrolmentId,
        CancellationToken cancellationToken = default) =>
        SendAsync<RaceResultResponse>(
            HttpMethod.Get, $"api/enrolments/{enrolmentId}/result", null, cancellationToken);

    public Task<ApiCallResult<RaceResultResponse>> CreateResultAsync(
        int enrolmentId,
        ResultRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<RaceResultResponse>(
            HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request, cancellationToken);

    public Task<ApiCallResult<RaceResultResponse>> UpdateResultAsync(
        int enrolmentId,
        ResultRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<RaceResultResponse>(
            HttpMethod.Put, $"api/enrolments/{enrolmentId}/result", request, cancellationToken);

    public Task<ApiCallResult<object>> DeleteResultAsync(
        int enrolmentId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(
            HttpMethod.Delete, $"api/enrolments/{enrolmentId}/result", null, cancellationToken);

    public Task<ApiCallResult<object>> LogoutAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<object>(HttpMethod.Post, "api/auth/logout", new { }, cancellationToken);

    private async Task<ApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string route,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, route);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ApiCallResult<T>(
                null,
                default,
                "The RaceDay API could not be reached. Check that it is running and try again.",
                IsConnectionFailure: true);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiCallResult<T>(
                null,
                default,
                "The RaceDay API did not respond in time. Please try again.",
                IsConnectionFailure: true);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return new ApiCallResult<T>(response.StatusCode, default, null);

                try
                {
                    var value = await response.Content.ReadFromJsonAsync<T>(
                        cancellationToken: cancellationToken);
                    return new ApiCallResult<T>(response.StatusCode, value, null);
                }
                catch (JsonException)
                {
                    return new ApiCallResult<T>(
                        response.StatusCode,
                        default,
                        "The API returned a response the MVC application could not read.");
                }
            }

            var message = await ReadErrorMessageAsync(response, route, cancellationToken);
            return new ApiCallResult<T>(response.StatusCode, default, message);
        }
    }

    private static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        string route,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return route.Equals("api/auth/login", StringComparison.OrdinalIgnoreCase)
                ? "Email or password is incorrect."
                : "Your API session has expired. Please sign in again.";
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return "You do not have permission to access this page.";

        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var propertyName in new[] { "message", "title", "detail" })
                {
                    if (document.RootElement.TryGetProperty(propertyName, out var value) &&
                        value.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        return value.GetString()!;
                    }
                }

                if (document.RootElement.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        if (field.Value.ValueKind == JsonValueKind.Array &&
                            field.Value.EnumerateArray().FirstOrDefault().ValueKind ==
                            JsonValueKind.String)
                        {
                            return field.Value[0].GetString()!;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Use the status fallback for non-JSON error responses.
        }

        return $"The RaceDay API returned HTTP {(int)response.StatusCode}. Please try again.";
    }
}

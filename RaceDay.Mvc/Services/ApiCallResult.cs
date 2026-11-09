using System.Net;

namespace RaceDay.Mvc.Services;

public sealed record ApiCallResult<T>(
    HttpStatusCode? StatusCode,
    T? Value,
    string? ErrorMessage,
    bool IsConnectionFailure = false)
{
    public bool IsSuccess =>
        StatusCode is not null && (int)StatusCode >= 200 && (int)StatusCode < 300;
}

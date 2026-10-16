
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class SessionAuthorizeAttribute
    : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles ?? Array.Empty<string>();
    }

    public Task OnAuthorizationAsync(
        AuthorizationFilterContext context)
    {
        // Read identity and role from the server-side session.
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        // No valid session means the user is not authenticated.
        if (userId is null || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Please log in to access this resource."
            });

            return Task.CompletedTask;
        }

        // If specific roles are required, check the stored role.
        if (_allowedRoles.Length > 0 &&
            !_allowedRoles.Contains(
                role,
                StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new
            {
                message = "You do not have permission to access this resource."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        // No result means authorization succeeded.
        return Task.CompletedTask;
    }
}

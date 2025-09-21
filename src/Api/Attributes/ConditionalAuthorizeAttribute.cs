using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Attributes;

/// <summary>
/// Authorization attribute that conditionally applies authorization based on environment.
/// Bypasses authorization in Testing environment for E2E tests.
/// </summary>
public class ConditionalAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _policy;

    public ConditionalAuthorizeAttribute(string? policy = null)
    {
        _policy = policy;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context?.HttpContext?.RequestServices == null)
        {
            context.Result = new UnauthorizedResult(); // Fail-safe: deny access if we can't determine context
            return;
        }

        try
        {
            var environment = context.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

            // Complete bypass for Testing environment - allow everything
            if (environment.IsEnvironment("Testing"))
            {
                // Additional safeguard: verify we're not in a production-like environment
                var httpContext = context.HttpContext;
                var host = httpContext.Request.Host.Host;

                // Deny if hostname suggests production (additional safety check)
                if (host.Contains("prod") || host.Contains("api.") || host.EndsWith(".com"))
                {
                    context.Result = new ForbidResult("Testing environment detected on production-like host");
                    return;
                }

                // Do nothing - allow the request to proceed
                return;
            }

            // In non-Testing environments, apply normal authorization
            var authorizationService = context.HttpContext.RequestServices
                .GetRequiredService<IAuthorizationService>();

            // Check if user is authenticated
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // If a policy is specified, check it
            if (!string.IsNullOrEmpty(_policy))
            {
                var authResult = authorizationService.AuthorizeAsync(
                    context.HttpContext.User, _policy).Result;

                if (!authResult.Succeeded)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
        catch (Exception)
        {
            // If anything fails, deny access for security (fail-closed)
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Attributes;

/// <summary>
/// Authorization attribute that conditionally applies authorization based on environment.
/// Bypasses authorization in Testing environment for E2E tests.
/// </summary>
public class ConditionalAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string? _policy;

    public ConditionalAuthorizeAttribute(string? policy = null)
    {
        _policy = policy;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context?.HttpContext?.RequestServices == null)
        {
            if (context != null)
            {
                context.Result = new UnauthorizedResult(); // Fail-safe: deny access if we can't determine context
            }
            return;
        }

        try
        {
            var environment = context.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

            // Only bypass authorization for E2E tests by checking environment variable
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

                // Check if authorization bypass is explicitly enabled for E2E tests
                var bypassAuth = Environment.GetEnvironmentVariable("BYPASS_AUTHORIZATION_FOR_E2E") == "true";

                if (bypassAuth)
                {
                    // Do nothing - allow the request to proceed for E2E tests only
                    return;
                }

                // For integration tests and other Testing scenarios, apply normal authorization
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
                var authResult = await authorizationService.AuthorizeAsync(
                    context.HttpContext.User, _policy);

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

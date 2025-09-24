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
        Console.WriteLine($"DEBUG: ConditionalAuthorizeAttribute called for {context?.HttpContext?.Request?.Path}");

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

            // Handle different testing scenarios with appropriate authorization logic
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

                // Check for complete E2E authorization bypass
                var bypassE2E = Environment.GetEnvironmentVariable("BYPASS_AUTHORIZATION_FOR_E2E") == "true";
                var isE2ETest = Environment.GetEnvironmentVariable("E2E_TEST_MODE") == "true";

                if (bypassE2E && isE2ETest)
                {
                    // Complete bypass for E2E tests - skip all authorization
                    // E2E tests focus on UI/UX flow and don't need complex auth scenarios
                    return;
                }

                // Check for integration test authorization bypass
                var bypassIntegration = Environment.GetEnvironmentVariable("BYPASS_AUTHORIZATION_FOR_INTEGRATION") == "true";

                if (bypassIntegration)
                {
                    // Check if this is specifically an authorization test that should enforce auth
                    var hasEnforceHeader = httpContext.Request.Headers.ContainsKey("X-Test-Enforce-Auth");
                    var hasEnforceEnvVar = Environment.GetEnvironmentVariable("ENFORCE_AUTH_FOR_TEST") == "true";
                    var enforceAuth = hasEnforceHeader || hasEnforceEnvVar;

                    // Debug logging
                    Console.WriteLine($"DEBUG: bypassIntegration={bypassIntegration}, hasEnforceHeader={hasEnforceHeader}, hasEnforceEnvVar={hasEnforceEnvVar}, enforceAuth={enforceAuth}");

                    if (enforceAuth)
                    {
                        // This is an authorization test - enforce normal authorization
                        Console.WriteLine("DEBUG: Enforcing authorization for test");
                        // Fall through to normal auth logic below
                    }
                    else
                    {
                        // For integration tests: bypass authorization for business logic tests
                        Console.WriteLine("DEBUG: Bypassing authorization for business logic test");
                        // Integration tests handle authentication through AuthenticationTestHelper
                        return;
                    }
                }

                // For other Testing scenarios, apply normal authorization
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

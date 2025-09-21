using System.Security.Claims;

namespace Api.Middleware;

/// <summary>
/// Middleware to bypass authentication for E2E tests in Testing environment
/// </summary>
public class E2ETestAuthBypassMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<E2ETestAuthBypassMiddleware> _logger;

    public E2ETestAuthBypassMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        ILogger<E2ETestAuthBypassMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only active in Testing environment
        if (_environment.EnvironmentName == "Testing")
        {
            var testModeHeader = context.Request.Headers["X-E2E-Test-Mode"].FirstOrDefault();
            var testRunId = context.Request.Headers["X-Test-Run-Id"].FirstOrDefault();

            if (testModeHeader == "active" || !string.IsNullOrEmpty(testRunId))
            {
                // Create a test user identity to bypass authorization
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "test-user"),
                    new Claim(ClaimTypes.Email, "test@example.com"),
                    new Claim(ClaimTypes.Role, "Admin"), // Give admin role for testing
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("test-mode", "true")
                };

                var identity = new ClaimsIdentity(claims, "E2ETest");
                var principal = new ClaimsPrincipal(identity);

                context.User = principal;

                _logger.LogDebug("E2E test mode active, bypassing authentication for {Path}", context.Request.Path);
            }
        }

        await _next(context);
    }
}
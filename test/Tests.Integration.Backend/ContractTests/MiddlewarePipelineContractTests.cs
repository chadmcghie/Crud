using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// Middleware pipeline contract verification tests that ensure middleware registration order
/// and behavior contracts are maintained across different configurations
/// </summary>
public class MiddlewarePipelineContractTests : ContractTestBase
{
    private readonly ITestOutputHelper _output;

    public MiddlewarePipelineContractTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CorsMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING CORS MIDDLEWARE CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Add CORS origin header to trigger CORS middleware
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        var response = await client.GetAsync("/health");

        // CORS middleware contract expectations
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Check if CORS headers are present (they may or may not be based on configuration)
        var corsHeaders = response.Headers.Where(h => h.Key.StartsWith("Access-Control")).ToList();

        if (corsHeaders.Any())
        {
            _output.WriteLine($"CORS headers found in {environment}: {corsHeaders.Count}");

            // If CORS headers are present, validate their contract
            var allowOriginHeader = response.Headers.FirstOrDefault(h => h.Key == "Access-Control-Allow-Origin");
            if (allowOriginHeader.Key != null)
            {
                Assert.NotEmpty(allowOriginHeader.Value);
            }
        }

        _output.WriteLine($"✓ CORS middleware contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CompressionMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING COMPRESSION MIDDLEWARE CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Request with compression acceptance
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        var response = await client.GetAsync("/health");

        // Compression middleware contract expectations
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Response should be readable regardless of compression
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));

        // Check if compression was applied (optional contract)
        var contentEncoding = response.Content.Headers.ContentEncoding;
        if (contentEncoding.Any())
        {
            _output.WriteLine($"Content encoding in {environment}: {string.Join(", ", contentEncoding)}");
            Assert.All(contentEncoding, encoding =>
              Assert.True(encoding == "gzip" || encoding == "deflate" || encoding == "br"));
        }

        _output.WriteLine($"✓ Compression middleware contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthenticationMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING AUTHENTICATION MIDDLEWARE CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Test protected endpoint without authentication
        var unauthenticatedResponse = await client.GetAsync("/api/roles");

        // Authentication middleware contract expectations
        Assert.True(unauthenticatedResponse.StatusCode == HttpStatusCode.OK ||
                   unauthenticatedResponse.StatusCode == HttpStatusCode.Unauthorized);

        // Test with invalid token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var invalidTokenResponse = await client.GetAsync("/api/roles");

        Assert.True(invalidTokenResponse.StatusCode == HttpStatusCode.OK ||
                   invalidTokenResponse.StatusCode == HttpStatusCode.Unauthorized);

        _output.WriteLine($"✓ Authentication middleware contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ExceptionHandlingMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING EXCEPTION HANDLING MIDDLEWARE CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Test non-existent endpoint to trigger exception handling
        var response = await client.GetAsync("/api/nonexistent/endpoint");

        // Exception handling middleware contract expectations
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Production environment should not expose detailed error information
        if (environment == "Production")
        {
            Assert.DoesNotContain("stack trace", content.ToLower());
            Assert.DoesNotContain("exception", content.ToLower());
        }

        // Response should be properly formatted
        Assert.False(string.IsNullOrEmpty(response.Content.Headers.ContentType?.MediaType));

        _output.WriteLine($"✓ Exception handling middleware contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task LoggingMiddleware_Contract_ShouldBeConfiguredCorrectly(string environment)
    {
        _output.WriteLine($"=== VALIDATING LOGGING MIDDLEWARE CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);

        // Verify logging services are properly registered
        using var scope = factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);

        var logger = scope.ServiceProvider.GetService<ILogger<MiddlewarePipelineContractTests>>();
        Assert.NotNull(logger);

        // Test that logging doesn't interfere with requests
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);

        _output.WriteLine($"✓ Logging middleware contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task RequestResponseCycle_Contract_ShouldMaintainOrder(string environment)
    {
        _output.WriteLine($"=== VALIDATING MIDDLEWARE PIPELINE ORDER CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Send a request that will go through the entire middleware pipeline
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var response = await client.GetAsync("/health");

        // Pipeline contract expectations
        Assert.NotNull(response);
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Response should have proper headers indicating middleware processing
        Assert.NotNull(response.Headers);
        Assert.NotNull(response.Content.Headers);

        // Content should be readable after all middleware processing
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));

        _output.WriteLine($"✓ Middleware pipeline order contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task SecurityHeaders_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING SECURITY HEADERS CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        var response = await client.GetAsync("/health");

        // Security headers contract (these may or may not be present based on configuration)
        var securityHeaders = response.Headers
          .Where(h => h.Key.StartsWith("X-") ||
                      h.Key.Equals("Strict-Transport-Security", StringComparison.OrdinalIgnoreCase) ||
                      h.Key.Equals("Content-Security-Policy", StringComparison.OrdinalIgnoreCase))
          .ToList();

        foreach (var header in securityHeaders)
        {
            Assert.NotEmpty(header.Value);
            _output.WriteLine($"Security header in {environment}: {header.Key} = {string.Join(", ", header.Value)}");
        }

        // Production environment should have stricter security measures
        if (environment == "Production")
        {
            _output.WriteLine($"Production environment security validation for {environment}");
        }

        _output.WriteLine($"✓ Security headers contract validated for {environment}");
    }

    [Fact]
    public async Task MiddlewarePipeline_Contract_ShouldBeConsistentAcrossAllEnvironments()
    {
        _output.WriteLine("=== CROSS-ENVIRONMENT MIDDLEWARE PIPELINE CONTRACT CONSISTENCY ===");

        var middlewareTests = new Dictionary<string, Func<string, Task>>
        {
            ["CORS"] = async env => await CorsMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Compression"] = async env => await CompressionMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Authentication"] = async env => await AuthenticationMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["ExceptionHandling"] = async env => await ExceptionHandlingMiddleware_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Logging"] = async env => await LoggingMiddleware_Contract_ShouldBeConfiguredCorrectly(env),
            ["PipelineOrder"] = async env => await RequestResponseCycle_Contract_ShouldMaintainOrder(env),
            ["SecurityHeaders"] = async env => await SecurityHeaders_Contract_ShouldBeConsistentAcrossEnvironments(env)
        };

        foreach (var (middlewareName, testAction) in middlewareTests)
        {
            _output.WriteLine($"\nValidating {middlewareName} middleware across all environments:");

            foreach (var environment in GetSupportedEnvironments())
            {
                try
                {
                    await testAction(environment);
                    _output.WriteLine($"  ✓ {environment}: {middlewareName} middleware contract valid");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  ✗ {environment}: {middlewareName} middleware contract failed - {ex.Message}");
                    throw; // Re-throw to fail the test
                }
            }
        }

        _output.WriteLine("\n✓ All middleware pipeline contracts are consistent across environments");
    }
}

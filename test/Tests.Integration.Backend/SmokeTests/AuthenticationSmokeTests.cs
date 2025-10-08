using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Features.Authentication;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Smoke tests for authentication endpoints across different configurations
/// Validates that authentication functionality works correctly in all environments
/// </summary>
public class AuthenticationSmokeTests : SmokeTestBase
{
    private readonly ITestOutputHelper _output;

    public AuthenticationSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthRegisterEndpoint_ShouldAcceptValidRegistration_WithinTimeLimit(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /api/auth/register endpoint in {environment} environment");

        var registerRequest = new RegisterUserCommand
        {
            Email = $"smoketest_{environment}_{Guid.NewGuid():N}@example.com",
            Password = "SmokeTest123!",
            FirstName = "Smoke",
            LastName = "Test"
        };

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Should either succeed or return a validation error (but endpoint should be available)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.BadRequest
            );

            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(content));

            // If successful, response should be JSON
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    Assert.Equal(JsonValueKind.Object, jsonResponse.ValueKind);
                }
                catch (JsonException)
                {
                    // If not JSON, at least verify we got a response
                    Assert.True(content.Length > 0);
                }
            }

        }, $"Registration endpoint in {environment}");

        Assert.True(executionTime.TotalSeconds < 10);

        _output.WriteLine($"Registration in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthLoginEndpoint_ShouldReturnUnauthorized_ForInvalidCredentials(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /api/auth/login endpoint with invalid credentials in {environment} environment");

        var loginRequest = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "InvalidPassword123!"
        };

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Should return unauthorized for invalid credentials
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            // Content might be empty for 401 responses, that's acceptable

        }, $"Login endpoint (invalid credentials) in {environment}");

        Assert.True(executionTime.TotalSeconds < 10);

        _output.WriteLine($"Login (invalid) in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthLoginEndpoint_ShouldAcceptValidData_Structure(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /api/auth/login endpoint structure in {environment} environment");

        var loginRequest = new LoginCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!"
        };

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Should not return method not allowed or not found (endpoint should exist)
            Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);

            // Expected responses: Unauthorized (user doesn't exist) or BadRequest (validation)
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.OK
            );

        }, $"Login endpoint structure test in {environment}");

        Assert.True(executionTime.TotalSeconds < 10);

        _output.WriteLine($"Login structure in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthMeEndpoint_ShouldReturnUnauthorized_WithoutToken(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /api/auth/me endpoint without authentication in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.GetAsync("/api/auth/me");

            // Should return unauthorized without authentication token
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }, $"Auth/me endpoint (no auth) in {environment}");

        Assert.True(executionTime.TotalSeconds < 5);

        _output.WriteLine($"Auth/me (no auth) in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthRefreshEndpoint_ShouldExist_AndReturnAppropriateResponse(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /api/auth/refresh endpoint existence in {environment} environment");

        var refreshRequest = new RefreshTokenCommand
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

            // Endpoint should exist (not return 404 or 405)
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);

            Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // Should return bad request or unauthorized for invalid token
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Unauthorized
            );

        }, $"Refresh endpoint test in {environment}");

        Assert.True(executionTime.TotalSeconds < 5);

        _output.WriteLine($"Refresh endpoint in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthLogoutEndpoint_ShouldExist_AndAcceptRequest(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /api/auth/logout endpoint in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.PostAsync("/api/auth/logout", null);

            // Endpoint should exist and accept requests
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);

            Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            // Should return OK or Unauthorized (depending on whether auth is required)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NoContent ||
                response.StatusCode == HttpStatusCode.Unauthorized
            );

        }, $"Logout endpoint test in {environment}");

        Assert.True(executionTime.TotalSeconds < 5);

        _output.WriteLine($"Logout endpoint in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Fact]
    public async Task AllEnvironments_AuthenticationEndpoints_ShouldCompleteWithin30Seconds()
    {
        // This test validates that all authentication smoke tests across environments complete within time constraint
        _output.WriteLine("Testing all environment authentication endpoints complete within 30-second constraint");

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var environment in GetSupportedEnvironments())
        {
            using var client = CreateClientForEnvironment(environment);

            // Test key authentication endpoints quickly
            var loginRequest = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            Assert.NotEqual(HttpStatusCode.NotFound, loginResponse.StatusCode);

            var meResponse = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

            _output.WriteLine($"Auth endpoints validated for {environment}: {totalStopwatch.ElapsedMilliseconds}ms elapsed");
        }

        totalStopwatch.Stop();

        // Validate overall time constraint
        Assert.True(totalStopwatch.Elapsed.TotalSeconds < 30);

        _output.WriteLine($"Total authentication test time: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}

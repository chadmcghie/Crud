using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Features.Authentication;
using FluentAssertions;
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
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,
          HttpStatusCode.Created,
          HttpStatusCode.BadRequest // Validation errors are acceptable in smoke tests
        );

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty($"Registration response should have content in {environment}");

            // If successful, response should be JSON
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    jsonResponse.ValueKind.Should().Be(JsonValueKind.Object,
                  $"Successful registration should return JSON object in {environment}");
                }
                catch (JsonException)
                {
                    // If not JSON, at least verify we got a response
                    content.Length.Should().BeGreaterThan(0);
                }
            }

        }, $"Registration endpoint in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"Registration should complete within 10 seconds in {environment} environment");

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
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
          $"Login with invalid credentials should return 401 in {environment} environment");

            var content = await response.Content.ReadAsStringAsync();
            // Content might be empty for 401 responses, that's acceptable

        }, $"Login endpoint (invalid credentials) in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"Login attempt should complete within 10 seconds in {environment} environment");

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
            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
          $"Login endpoint should accept POST method in {environment} environment");

            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
          $"Login endpoint should exist in {environment} environment");

            // Expected responses: Unauthorized (user doesn't exist) or BadRequest (validation)
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.Unauthorized,
          HttpStatusCode.BadRequest,
          HttpStatusCode.OK // If the user happens to exist
        );

        }, $"Login endpoint structure test in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"Login structure test should complete quickly in {environment} environment");

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
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
          $"/api/auth/me should require authentication in {environment} environment");

        }, $"Auth/me endpoint (no auth) in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Auth/me without token should be very fast in {environment} environment");

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
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
          $"Refresh endpoint should exist in {environment} environment");

            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
          $"Refresh endpoint should accept POST in {environment} environment");

            // Should return bad request or unauthorized for invalid token
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.BadRequest,
          HttpStatusCode.Unauthorized
        );

        }, $"Refresh endpoint test in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Refresh endpoint test should be fast in {environment} environment");

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
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
          $"Logout endpoint should exist in {environment} environment");

            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
          $"Logout endpoint should accept POST in {environment} environment");

            // Should return OK or Unauthorized (depending on whether auth is required)
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,
          HttpStatusCode.NoContent,
          HttpStatusCode.Unauthorized
        );

        }, $"Logout endpoint test in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Logout endpoint test should be fast in {environment} environment");

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
            loginResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

            var meResponse = await client.GetAsync("/api/auth/me");
            meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _output.WriteLine($"Auth endpoints validated for {environment}: {totalStopwatch.ElapsedMilliseconds}ms elapsed");
        }

        totalStopwatch.Stop();

        // Validate overall time constraint
        totalStopwatch.Elapsed.TotalSeconds.Should().BeLessThan(30,
          "All environment authentication tests should complete within 30 seconds total");

        _output.WriteLine($"Total authentication test time: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Smoke tests for critical API endpoints across different configurations
/// Validates that essential CRUD operations work correctly within 30-second time constraints
/// </summary>
public class CriticalApiEndpointSmokeTests : SmokeTestBase
{
    private readonly ITestOutputHelper _output;

    public CriticalApiEndpointSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CoreCrudEndpoints_ShouldBeAccessible_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing core CRUD endpoints accessibility in {environment} environment");

        // Critical endpoints that must be available
        var criticalEndpoints = new[]
        {
      "/api/roles",
      "/api/people",
      "/api/walls",
      "/api/windows"
    };

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            // Use authenticated client for testing CRUD endpoints
            var adapter = new SmokeTestFactoryAdapter(factory);
            var authenticatedClient = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);

            foreach (var endpoint in criticalEndpoints)
            {
                var response = await authenticatedClient.GetAsync(endpoint);

                // Endpoint should be accessible and return OK
                response.StatusCode.Should().Be(HttpStatusCode.OK,
              $"{endpoint} should be accessible in {environment} environment");

                // Should return JSON content
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
              $"{endpoint} should return JSON in {environment} environment");

                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty($"{endpoint} should return content in {environment}");

                // Validate JSON structure
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    jsonResponse.ValueKind.Should().Be(JsonValueKind.Array,
                  $"{endpoint} should return JSON array in {environment} environment");
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"{endpoint} returned invalid JSON in {environment}: {ex.Message}");
                }

                _output.WriteLine($"✓ {endpoint} accessible in {environment}");
            }

        }, $"Core CRUD endpoints in {environment}");

        // All endpoints should be tested within time limit
        executionTime.TotalSeconds.Should().BeLessThan(15,
          $"All core endpoints should be accessible within 15 seconds in {environment} environment");

        _output.WriteLine($"Core endpoints in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task RolesEndpoint_ShouldSupportBasicCrud_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing roles endpoint CRUD operations in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var adapter = new SmokeTestFactoryAdapter(factory);
            var authenticatedClient = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);

            // Test GET (list)
            var getResponse = await authenticatedClient.GetAsync("/api/roles");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Test POST (create) with minimal data
            var createRequest = new { Name = $"SmokeTestRole_{environment}_{Guid.NewGuid().ToString("N")[..8]}" };
            var postResponse = await authenticatedClient.PostAsJsonAsync("/api/roles", createRequest);

            // Should either succeed or return validation error
            postResponse.StatusCode.Should().BeOneOf(
          HttpStatusCode.Created,
          HttpStatusCode.BadRequest // Validation errors acceptable
        );

            if (postResponse.StatusCode == HttpStatusCode.Created)
            {
                var content = await postResponse.Content.ReadAsStringAsync();
                var createdRole = JsonSerializer.Deserialize<JsonElement>(content);

                createdRole.TryGetProperty("id", out var idProperty).Should().BeTrue(
              $"Created role should have ID in {environment}");

                _output.WriteLine($"✓ Role created successfully in {environment}");
            }
            else
            {
                _output.WriteLine($"✓ Role creation validation working in {environment}");
            }

        }, $"Roles CRUD operations in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"Roles CRUD test should complete within 10 seconds in {environment} environment");

        _output.WriteLine($"Roles CRUD in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task PeopleEndpoint_ShouldSupportBasicOperations_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing people endpoint basic operations in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var adapter = new SmokeTestFactoryAdapter(factory);
            var authenticatedClient = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);

            // Test GET (list)
            var getResponse = await authenticatedClient.GetAsync("/api/people");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await getResponse.Content.ReadAsStringAsync();
            var peopleArray = JsonSerializer.Deserialize<JsonElement>(content);
            peopleArray.ValueKind.Should().Be(JsonValueKind.Array);

            _output.WriteLine($"✓ People endpoint accessible and returns array in {environment}");

            // Test POST structure (without necessarily creating)
            var createRequest = new { FullName = $"SmokeTest Person {environment}" };
            var postResponse = await authenticatedClient.PostAsJsonAsync("/api/people", createRequest);

            // Should respond appropriately (either success or validation error)
            postResponse.StatusCode.Should().BeOneOf(
          HttpStatusCode.Created,
          HttpStatusCode.BadRequest,
          HttpStatusCode.UnprocessableEntity
        );

            _output.WriteLine($"✓ People POST endpoint responds appropriately in {environment}");

        }, $"People operations in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"People operations should complete within 10 seconds in {environment} environment");

        _output.WriteLine($"People operations in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ApiErrorHandling_ShouldWorkCorrectly_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing API error handling in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var adapter = new SmokeTestFactoryAdapter(factory);
            var authenticatedClient = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);

            // Test 404 handling
            var notFoundResponse = await authenticatedClient.GetAsync("/api/nonexistent");
            notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
          $"Non-existent endpoints should return 404 in {environment}");

            // Test invalid method
            var invalidMethodResponse = await authenticatedClient.PatchAsync("/api/roles", null);
            invalidMethodResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
          $"Invalid methods should return 405 in {environment}");

            // Test malformed JSON
            var malformedContent = new StringContent("{ invalid json", System.Text.Encoding.UTF8, "application/json");
            var malformedResponse = await authenticatedClient.PostAsync("/api/roles", malformedContent);
            malformedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
          $"Malformed JSON should return 400 in {environment}");

            _output.WriteLine($"✓ Error handling working correctly in {environment}");

        }, $"Error handling in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(8,
          $"Error handling tests should complete quickly in {environment} environment");

        _output.WriteLine($"Error handling in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ApiResponsiveness_ShouldMeetPerformanceThresholds_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing API responsiveness thresholds in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var adapter = new SmokeTestFactoryAdapter(factory);
            var authenticatedClient = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);

            // Test multiple quick requests to verify responsiveness
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(authenticatedClient.GetAsync("/api/roles"));
            }

            var responses = await Task.WhenAll(tasks);

            // All requests should succeed
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK,
              $"Concurrent requests should succeed in {environment}");
            }

            _output.WriteLine($"✓ Handled 5 concurrent requests successfully in {environment}");

        }, $"API responsiveness in {environment}");

        // Performance threshold for smoke tests
        executionTime.TotalSeconds.Should().BeLessThan(8,
          $"API responsiveness test should complete within 8 seconds in {environment} environment");

        _output.WriteLine($"API responsiveness in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Fact]
    public async Task AllEnvironments_CriticalEndpoints_ShouldCompleteWithin30Seconds()
    {
        // This test validates the overall 30-second constraint for critical API validation
        _output.WriteLine("Testing all environments critical endpoints complete within 30-second constraint");

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var environment in GetSupportedEnvironments())
        {
            using var factory = CreateFactoryForEnvironment(environment);
            var adapter = new SmokeTestFactoryAdapter(factory);
            var authenticatedClient = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);

            // Test essential endpoints quickly
            var coreEndpoints = new[] { "/api/roles", "/api/people", "/health" };

            foreach (var endpoint in coreEndpoints)
            {
                var response = await authenticatedClient.GetAsync(endpoint);
                response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
            }

            _output.WriteLine($"Critical endpoints validated for {environment}: {totalStopwatch.ElapsedMilliseconds}ms elapsed");
        }

        totalStopwatch.Stop();

        // Validate overall time constraint
        totalStopwatch.Elapsed.TotalSeconds.Should().BeLessThan(30,
          "All environment critical endpoint tests should complete within 30 seconds total");

        _output.WriteLine($"Total critical endpoints test time: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}

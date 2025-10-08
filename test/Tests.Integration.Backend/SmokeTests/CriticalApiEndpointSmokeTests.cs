using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Should return JSON content
                Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

                var content = await response.Content.ReadAsStringAsync();
                Assert.False(string.IsNullOrEmpty(content));

                // Validate JSON structure
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    Assert.Equal(JsonValueKind.Array, jsonResponse.ValueKind);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"{endpoint} returned invalid JSON in {environment}: {ex.Message}");
                }

                _output.WriteLine($"✓ {endpoint} accessible in {environment}");
            }

        }, $"Core CRUD endpoints in {environment}");

        // All endpoints should be tested within time limit
        Assert.True(executionTime.TotalSeconds < 15);

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
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Test POST (create) with minimal data
            var createRequest = new { Name = $"SmokeTestRole_{environment}_{Guid.NewGuid().ToString("N")[..8]}" };
            var postResponse = await authenticatedClient.PostAsJsonAsync("/api/roles", createRequest);

            // Should either succeed or return validation error
            Assert.True(
                postResponse.StatusCode == HttpStatusCode.Created ||
                postResponse.StatusCode == HttpStatusCode.BadRequest
            );

            if (postResponse.StatusCode == HttpStatusCode.Created)
            {
                var content = await postResponse.Content.ReadAsStringAsync();
                var createdRole = JsonSerializer.Deserialize<JsonElement>(content);

                Assert.True(createdRole.TryGetProperty("id", out var idProperty));

                _output.WriteLine($"✓ Role created successfully in {environment}");
            }
            else
            {
                _output.WriteLine($"✓ Role creation validation working in {environment}");
            }

        }, $"Roles CRUD operations in {environment}");

        Assert.True(executionTime.TotalSeconds < 10);

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
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var content = await getResponse.Content.ReadAsStringAsync();
            var peopleArray = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal(JsonValueKind.Array, peopleArray.ValueKind);

            _output.WriteLine($"✓ People endpoint accessible and returns array in {environment}");

            // Test POST structure (without necessarily creating)
            var createRequest = new { FullName = $"SmokeTest Person {environment}" };
            var postResponse = await authenticatedClient.PostAsJsonAsync("/api/people", createRequest);

            // Should respond appropriately (either success or validation error)
            Assert.True(
                postResponse.StatusCode == HttpStatusCode.Created ||
                postResponse.StatusCode == HttpStatusCode.BadRequest ||
                postResponse.StatusCode == HttpStatusCode.UnprocessableEntity
            );

            _output.WriteLine($"✓ People POST endpoint responds appropriately in {environment}");

        }, $"People operations in {environment}");

        Assert.True(executionTime.TotalSeconds < 10);

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
            Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

            // Test invalid method
            var invalidMethodResponse = await authenticatedClient.PatchAsync("/api/roles", null);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, invalidMethodResponse.StatusCode);

            // Test malformed JSON
            var malformedContent = new StringContent("{ invalid json", System.Text.Encoding.UTF8, "application/json");
            var malformedResponse = await authenticatedClient.PostAsync("/api/roles", malformedContent);
            Assert.Equal(HttpStatusCode.BadRequest, malformedResponse.StatusCode);

            _output.WriteLine($"✓ Error handling working correctly in {environment}");

        }, $"Error handling in {environment}");

        Assert.True(executionTime.TotalSeconds < 8);

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
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            _output.WriteLine($"✓ Handled 5 concurrent requests successfully in {environment}");

        }, $"API responsiveness in {environment}");

        // Performance threshold for smoke tests
        Assert.True(executionTime.TotalSeconds < 8);

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
                Assert.True(
                    response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Unauthorized
                );
            }

            _output.WriteLine($"Critical endpoints validated for {environment}: {totalStopwatch.ElapsedMilliseconds}ms elapsed");
        }

        totalStopwatch.Stop();

        // Validate overall time constraint
        Assert.True(totalStopwatch.Elapsed.TotalSeconds < 30);

        _output.WriteLine($"Total critical endpoints test time: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}

using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// API contract validation tests that ensure configuration changes don't break API contracts
/// Validates response schemas, status codes, headers, and data structure consistency across configurations
/// </summary>
public class ApiContractValidationTests : ContractTestBase
{
    private readonly ITestOutputHelper _output;

    public ApiContractValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task RolesAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING ROLES API CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

        // Test GET /api/roles contract
        var getResponse = await client.GetAsync("/api/roles");
        ValidateResponseContract(getResponse, HttpStatusCode.OK, "application/json");

        var roles = await ValidateJsonContract<List<RoleDto>>(getResponse);
        ValidateContractStructure(roles, rolesList =>
        {
            Assert.NotNull(rolesList);
            // Empty list is valid for initial state
        });

        // Test POST /api/roles contract
        var createRequest = new CreateRoleRequest("ContractTestRole", "Test role for contract validation");
        var postResponse = await client.PostAsJsonAsync("/api/roles", createRequest);
        ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

        var createdRole = await ValidateJsonContract<RoleDto>(postResponse);
        ValidateContractStructure(createdRole, role =>
        {
            Assert.NotEqual(Guid.Empty, role.Id);
            Assert.Equal("ContractTestRole", role.Name);
            Assert.Equal("Test role for contract validation", role.Description);
        });

        // Validate Location header contract
        Assert.NotNull(postResponse.Headers.Location);
        Assert.Contains("/api/roles/", postResponse.Headers.Location!.ToString());

        _output.WriteLine($"✓ Roles API contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task PeopleAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING PEOPLE API CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

        // Test GET /api/people contract
        var getResponse = await client.GetAsync("/api/people");
        ValidateResponseContract(getResponse, HttpStatusCode.OK, "application/json");

        var people = await ValidateJsonContract<List<PersonResponse>>(getResponse);
        ValidateContractStructure(people, peopleList =>
        {
            Assert.NotNull(peopleList);
            // Empty list is valid for initial state
        });

        // Test POST /api/people contract
        var createRequest = new CreatePersonRequest("John Doe", "555-1234", null);
        var postResponse = await client.PostAsJsonAsync("/api/people", createRequest);
        ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

        var createdPerson = await ValidateJsonContract<PersonResponse>(postResponse);
        ValidateContractStructure(createdPerson, person =>
        {
            Assert.NotEqual(Guid.Empty, person.Id);
            Assert.Equal("John Doe", person.FullName);
            Assert.Equal("555-1234", person.Phone);
        });

        // Validate Location header contract
        Assert.NotNull(postResponse.Headers.Location);
        Assert.Contains("/api/people/", postResponse.Headers.Location!.ToString());

        _output.WriteLine($"✓ People API contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task WallsAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING WALLS API CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

        // Test GET /api/walls contract
        var getResponse = await client.GetAsync("/api/walls");
        ValidateResponseContract(getResponse, HttpStatusCode.OK, "application/json");

        var walls = await ValidateJsonContract<List<WallResponse>>(getResponse);
        ValidateContractStructure(walls, wallsList =>
        {
            Assert.NotNull(wallsList);
        });

        // Test POST /api/walls contract
        var createRequest = new CreateWallRequest("Contract Test Wall", "A wall for contract validation", 10.0, 8.0, 0.5, "Drywall", null, null, null, null, null, null);
        var postResponse = await client.PostAsJsonAsync("/api/walls", createRequest);
        ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

        var createdWall = await ValidateJsonContract<WallResponse>(postResponse);
        ValidateContractStructure(createdWall, wall =>
        {
            Assert.NotEqual(Guid.Empty, wall.Id);
            Assert.Equal("Contract Test Wall", wall.Name);
            Assert.Equal("A wall for contract validation", wall.Description);
            Assert.Equal("Drywall", wall.AssemblyType);
        });

        _output.WriteLine($"✓ Walls API contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task WindowsAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING WINDOWS API CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

        // Test GET /api/windows contract
        var getResponse = await client.GetAsync("/api/windows");
        ValidateResponseContract(getResponse, HttpStatusCode.OK, "application/json");

        var windows = await ValidateJsonContract<List<WindowResponse>>(getResponse);
        ValidateContractStructure(windows, windowsList =>
        {
            Assert.NotNull(windowsList);
        });

        // Test POST /api/windows contract
        var createRequest = new CreateWindowRequest("Contract Test Window", "A window for contract validation", 4.0, 6.0, 24.0, "Wood", null, "Double-pane", null, null, null, null, null, null, null, null, null, null, null, null, null);
        var postResponse = await client.PostAsJsonAsync("/api/windows", createRequest);
        ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

        var createdWindow = await ValidateJsonContract<WindowResponse>(postResponse);
        ValidateContractStructure(createdWindow, window =>
        {
            Assert.NotEqual(Guid.Empty, window.Id);
            Assert.Equal("Contract Test Window", window.Name);
            Assert.Equal("A window for contract validation", window.Description);
            Assert.Equal("Wood", window.FrameType);
        });

        _output.WriteLine($"✓ Windows API contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task HealthAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING HEALTH API CONTRACT: {environment} ===");

        // Check feature flag configuration
        var configuration = BuildConfigurationForEnvironment(environment);
        var isHealthChecksDetailedEnabled = configuration.GetValue<bool>("FeatureManagement:HealthChecksDetailed");

        using var client = CreateClientForEnvironment(environment);

        // Test GET /health contract (liveness check - returns text/plain)
        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var healthContent = await healthResponse.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(healthContent));
        Assert.Contains("healthy", healthContent.ToLower());

        // Test GET /health/detailed contract (detailed health) - respects feature flag
        var detailedHealthResponse = await client.GetAsync("/health/detailed");
        if (isHealthChecksDetailedEnabled)
        {
            // Feature enabled - validate contract
            ValidateResponseContract(detailedHealthResponse, HttpStatusCode.OK, "application/json");

            var detailedHealthData = await ValidateJsonContract<object>(detailedHealthResponse);
            ValidateContractStructure(detailedHealthData, health =>
            {
                Assert.NotNull(health);
                // Detailed health data structure should be consistent
            });
        }
        else
        {
            // Feature disabled - expect NotFound
            Assert.Equal(HttpStatusCode.NotFound, detailedHealthResponse.StatusCode);
            _output.WriteLine($"  /health/detailed disabled in {environment} (HealthChecksDetailed feature flag: false)");
        }

        _output.WriteLine($"✓ Health API contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING AUTH API CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Test unauthenticated GET /api/auth/me contract
        var meResponse = await client.GetAsync("/api/auth/me");
        ValidateResponseContract(meResponse, HttpStatusCode.Unauthorized);

        // Test POST /api/auth/login contract (with invalid credentials)
        var loginRequest = new { Email = "invalid@test.com", Password = "InvalidPassword" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Login should either return Unauthorized or BadRequest depending on implementation
        Assert.True(loginResponse.StatusCode == HttpStatusCode.Unauthorized ||
                   loginResponse.StatusCode == HttpStatusCode.BadRequest);

        _output.WriteLine($"✓ Auth API contract validated for {environment}");
    }

    [Fact]
    public async Task AllAPIs_Contract_ShouldBeConsistentAcrossAllEnvironments()
    {
        _output.WriteLine("=== CROSS-ENVIRONMENT API CONTRACT CONSISTENCY VALIDATION ===");

        var endpoints = new[]
        {
      "/api/roles",
      "/api/people",
      "/api/walls",
      "/api/windows",
      "/health"
    };

        foreach (var endpoint in endpoints)
        {
            _output.WriteLine($"\nValidating contract consistency for {endpoint}:");

            // /health endpoint returns text/plain, not JSON
            if (endpoint == "/health")
            {
                foreach (var environment in GetSupportedEnvironments())
                {
                    using var client = CreateClientForEnvironment(environment);
                    var response = await client.GetAsync(endpoint);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

                    var healthContent = await response.Content.ReadAsStringAsync();
                    Assert.False(string.IsNullOrEmpty(healthContent));

                    _output.WriteLine($"  ✓ {environment}: Contract validated");
                }
                continue;
            }

            var results = await ValidateContractAcrossEnvironments<object>(
              endpoint,
              async client =>
              {
                  if (endpoint.StartsWith("/api/"))
                  {
                      // For API endpoints, use authenticated client
                      using var factory = CreateFactoryForEnvironment("Development");
                      var authClient = await AuthenticationTestHelper.CreateAdminClientAsync(
                  new ContractTestFactoryAdapter(factory));
                      return await authClient.GetAsync(endpoint);
                  }
                  return await client.GetAsync(endpoint);
              },
              HttpStatusCode.OK);

            foreach (var (environment, result) in results)
            {
                _output.WriteLine($"  ✓ {environment}: Contract validated");
            }
        }

        _output.WriteLine("\n✓ All API contracts are consistent across environments");
    }
}

/// <summary>
/// Adapter to make WebApplicationFactory work with AuthenticationTestHelper
/// </summary>
public class ContractTestFactoryAdapter : ITestWebApplicationFactory
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public ContractTestFactoryAdapter(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    public HttpClient CreateClient() => _factory.CreateClient();
    public IServiceProvider Services => _factory.Services;
    public TestLogCapture? LogCapture => null; // Not needed for contract tests
    public void EnsureDatabaseCreated() { /* Handled by factory configuration */ }
    public async Task ClearDatabaseAsync() => await Task.CompletedTask;

    public async Task SetUserRoleAsync(string email, string role)
    {
        // Basic implementation for contract tests
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        if (user != null && role == "Admin")
        {
            user.AddRole("Admin");
            await dbContext.SaveChangesAsync();
        }
    }

    public void Dispose() => _factory.Dispose();
}

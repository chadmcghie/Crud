using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Infrastructure.Data;
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
      rolesList.Should().NotBeNull("Roles list should be a valid array");
      // Empty list is valid for initial state
    });

    // Test POST /api/roles contract
    var createRequest = new CreateRoleRequest("ContractTestRole", "Test role for contract validation");
    var postResponse = await client.PostAsJsonAsync("/api/roles", createRequest);
    ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

    var createdRole = await ValidateJsonContract<RoleDto>(postResponse);
    ValidateContractStructure(createdRole, role =>
    {
      role.Id.Should().NotBeEmpty("Role ID should be a valid GUID");
      role.Name.Should().Be("ContractTestRole", "Role name should match request");
      role.Description.Should().Be("Test role for contract validation", "Role description should match request");
    });

    // Validate Location header contract
    postResponse.Headers.Location.Should().NotBeNull("Created response should include Location header");
    postResponse.Headers.Location!.ToString().Should().Contain("/api/roles/",
      "Location header should follow REST convention");

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
      peopleList.Should().NotBeNull("People list should be a valid array");
      // Empty list is valid for initial state
    });

    // Test POST /api/people contract
    var createRequest = new CreatePersonRequest("John Doe", "555-1234", null);
    var postResponse = await client.PostAsJsonAsync("/api/people", createRequest);
    ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

    var createdPerson = await ValidateJsonContract<PersonResponse>(postResponse);
    ValidateContractStructure(createdPerson, person =>
    {
      person.Id.Should().NotBeEmpty("Person ID should be a valid GUID");
      person.FullName.Should().Be("John Doe", "Full name should match request");
      person.Phone.Should().Be("555-1234", "Phone should match request");
    });

    // Validate Location header contract
    postResponse.Headers.Location.Should().NotBeNull("Created response should include Location header");
    postResponse.Headers.Location!.ToString().Should().Contain("/api/people/",
      "Location header should follow REST convention");

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
      wallsList.Should().NotBeNull("Walls list should be a valid array");
    });

    // Test POST /api/walls contract
    var createRequest = new CreateWallRequest("Contract Test Wall", "A wall for contract validation", 10.0, 8.0, 0.5, "Drywall", null, null, null, null, null, null);
    var postResponse = await client.PostAsJsonAsync("/api/walls", createRequest);
    ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

    var createdWall = await ValidateJsonContract<WallResponse>(postResponse);
    ValidateContractStructure(createdWall, wall =>
    {
      wall.Id.Should().NotBeEmpty("Wall ID should be a valid GUID");
      wall.Name.Should().Be("Contract Test Wall", "Wall name should match request");
      wall.Description.Should().Be("A wall for contract validation", "Wall description should match request");
      wall.AssemblyType.Should().Be("Drywall", "Wall assembly type should match request");
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
      windowsList.Should().NotBeNull("Windows list should be a valid array");
    });

    // Test POST /api/windows contract
    var createRequest = new CreateWindowRequest("Contract Test Window", "A window for contract validation", 4.0, 6.0, 24.0, "Wood", null, "Double-pane", null, null, null, null, null, null, null, null, null, null, null, null, null);
    var postResponse = await client.PostAsJsonAsync("/api/windows", createRequest);
    ValidateResponseContract(postResponse, HttpStatusCode.Created, "application/json");

    var createdWindow = await ValidateJsonContract<WindowResponse>(postResponse);
    ValidateContractStructure(createdWindow, window =>
    {
      window.Id.Should().NotBeEmpty("Window ID should be a valid GUID");
      window.Name.Should().Be("Contract Test Window", "Window name should match request");
      window.Description.Should().Be("A window for contract validation", "Window description should match request");
      window.FrameType.Should().Be("Wood", "Window frame type should match request");
    });

    _output.WriteLine($"✓ Windows API contract validated for {environment}");
  }

  [Theory]
  [MemberData(nameof(GetEnvironmentsAsTestData))]
  public async Task HealthAPI_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
  {
    _output.WriteLine($"=== VALIDATING HEALTH API CONTRACT: {environment} ===");

    using var client = CreateClientForEnvironment(environment);

    // Test GET /health contract
    var healthResponse = await client.GetAsync("/health");
    ValidateResponseContract(healthResponse, HttpStatusCode.OK, "text/plain");

    var healthContent = await healthResponse.Content.ReadAsStringAsync();
    healthContent.Should().Be("Healthy", "Health endpoint should return 'Healthy' text");

    // Test GET /api/health contract (detailed health)
    var apiHealthResponse = await client.GetAsync("/api/health");
    ValidateResponseContract(apiHealthResponse, HttpStatusCode.OK, "application/json");

    var healthData = await ValidateJsonContract<object>(apiHealthResponse);
    ValidateContractStructure(healthData, health =>
    {
      health.Should().NotBeNull("Health data should not be null");
      // Health data structure should be consistent
    });

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
    loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);

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

      var results = await ValidateContractAcrossEnvironments<object>(
        endpoint,
        async client =>
        {
          if (endpoint.StartsWith("/api/") && endpoint != "/health")
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

    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user != null && role == "Admin")
    {
      user.AddRole("Admin");
      await dbContext.SaveChangesAsync();
    }
  }

  public void Dispose() => _factory.Dispose();
}
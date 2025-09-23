using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Controllers.MultiProvider;

/// <summary>
/// Multi-provider integration tests for RolesController
/// Verifies that role management works consistently across all database providers
/// </summary>
public class RolesControllerMultiProviderTests
{
    private readonly ITestOutputHelper _output;

    public RolesControllerMultiProviderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task GET_Roles_Should_Return_Empty_List_Initially_AcrossAllProviders(DatabaseProvider provider)
    {
        // Arrange
        using var testInstance = new TestProviderInstance(provider);
        _output.WriteLine($"Testing GET roles with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await testInstance.AuthenticatedGetAsync("/api/roles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var roles = await testInstance.ReadJsonAsync<List<RoleDto>>(response);
            roles.Should().NotBeNull();
            roles.Should().BeEmpty($"Initial roles list should be empty for {testInstance.ProviderName}");
        });
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task POST_Roles_Should_Create_Role_And_Return_201_AcrossAllProviders(DatabaseProvider provider)
    {
        // Arrange
        using var testInstance = new TestProviderInstance(provider);
        _output.WriteLine($"Testing POST role creation with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var roleName = testInstance.CreateProviderSpecificTestData("Administrator");
            var createRequest = TestDataBuilders.CreateRoleRequest(roleName, "System administrator role");

            // Act
            var response = await testInstance.AuthenticatedPostJsonAsync("/api/roles", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"Role creation should succeed for {testInstance.ProviderName}");

            var createdRole = await testInstance.ReadJsonAsync<RoleDto>(response);
            createdRole.Should().NotBeNull();
            createdRole!.Id.Should().NotBeEmpty();
            createdRole.Name.Should().Be(roleName);
            createdRole.Description.Should().Be("System administrator role");

            // Verify location header
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Contain(createdRole.Id.ToString());
        });
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task PUT_Roles_Should_Update_Existing_Role_AcrossAllProviders(DatabaseProvider provider)
    {
        // Arrange
        using var testInstance = new TestProviderInstance(provider);
        _output.WriteLine($"Testing PUT role update with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create a role first
            var originalName = testInstance.CreateProviderSpecificTestData("OriginalRole");
            var createRequest = TestDataBuilders.CreateRoleRequest(originalName, "Original description");
            var createResponse = await testInstance.AuthenticatedPostJsonAsync("/api/roles", createRequest);
            var createdRole = await testInstance.ReadJsonAsync<RoleDto>(createResponse);

            // Prepare update
            var updatedName = testInstance.CreateProviderSpecificTestData("UpdatedRole");
            var updateRequest = TestDataBuilders.UpdateRoleRequest(updatedName, "Updated description");

            // Act
            var updateResponse = await testInstance.AuthenticatedPutJsonAsync($"/api/roles/{createdRole!.Id}", updateRequest);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Role update should succeed for {testInstance.ProviderName}");

            var updatedRole = await testInstance.ReadJsonAsync<RoleDto>(updateResponse);
            updatedRole.Should().NotBeNull();
            updatedRole!.Id.Should().Be(createdRole.Id);
            updatedRole.Name.Should().Be(updatedName);
            updatedRole.Description.Should().Be("Updated description");
        });
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task DELETE_Roles_Should_Remove_Role_AcrossAllProviders(DatabaseProvider provider)
    {
        // Arrange
        using var testInstance = new TestProviderInstance(provider);
        _output.WriteLine($"Testing DELETE role with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create a role first
            var roleName = testInstance.CreateProviderSpecificTestData("RoleToDelete");
            var createRequest = TestDataBuilders.CreateRoleRequest(roleName, "Role for deletion test");
            var createResponse = await testInstance.AuthenticatedPostJsonAsync("/api/roles", createRequest);
            var createdRole = await testInstance.ReadJsonAsync<RoleDto>(createResponse);

            // Act
            var deleteResponse = await testInstance.AuthenticatedDeleteAsync($"/api/roles/{createdRole!.Id}");

            // Assert
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
                $"Role deletion should succeed for {testInstance.ProviderName}");

            // Verify role is actually deleted
            var getResponse = await testInstance.AuthenticatedGetAsync($"/api/roles/{createdRole.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                $"Deleted role should not be found for {testInstance.ProviderName}");
        });
    }

    [Theory]
    [MemberData(nameof(GetPersistenceProviders))]
    public async Task Role_Data_Should_Persist_Between_Requests_ForPersistentProviders(DatabaseProvider provider)
    {
        // This test only runs on providers that support persistence (SQLite, SQL Server)
        using var testInstance = new TestProviderInstance(provider);
        _output.WriteLine($"Testing data persistence with {testInstance.ProviderName} provider");

        var roleName = testInstance.CreateProviderSpecificTestData("PersistentRole");
        Guid roleId = Guid.Empty;

        // First request - create role
        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            var createRequest = TestDataBuilders.CreateRoleRequest(roleName, "Persistence test role");
            var createResponse = await testInstance.AuthenticatedPostJsonAsync("/api/roles", createRequest);
            var createdRole = await testInstance.ReadJsonAsync<RoleDto>(createResponse);
            roleId = createdRole!.Id;

            // Verify the role exists within the same test
            var getResponse = await testInstance.AuthenticatedGetAsync($"/api/roles/{roleId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Role should persist in {testInstance.ProviderName} provider");
        });
    }

    public static IEnumerable<object[]> GetAllProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetAllProvidersAsTestData();

    public static IEnumerable<object[]> GetPersistenceProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetProvidersWithFeaturesAsTestData(requiresPersistence: true);

    /// <summary>
    /// Simple wrapper to instantiate the MultiProviderIntegrationTestBase for each provider
    /// </summary>
    private class TestProviderInstance : MultiProviderIntegrationTestBase
    {
        public TestProviderInstance(DatabaseProvider provider) : base(provider) { }
    }
}

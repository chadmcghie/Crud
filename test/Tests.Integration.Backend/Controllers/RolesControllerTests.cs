using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

public class RolesControllerTests : IntegrationTestBase
{
    public RolesControllerTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_Roles_Should_Return_Empty_List_Initially()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await AuthenticatedGetAsync("/api/roles");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var roles = await ReadJsonAsync<List<RoleDto>>(response);
            Assert.NotNull(roles);
            Assert.Empty(roles);
        });
    }

    [Fact]
    public async Task POST_Roles_Should_Create_Role_And_Return_201()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var createRequest = TestDataBuilders.CreateRoleRequest("Administrator", "System administrator role");

            // Act
            var response = await AuthenticatedPostJsonAsync("/api/roles", createRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdRole = await ReadJsonAsync<RoleDto>(response);

            Assert.NotNull(createdRole);
            Assert.NotEqual(Guid.Empty, createdRole!.Id);
            Assert.Equal("Administrator", createdRole.Name);
            Assert.Equal("System administrator role", createdRole.Description);

            // Verify location header
            Assert.NotNull(response.Headers.Location);
            Assert.Contains($"/api/roles/{createdRole.Id}".ToLowerInvariant(), response.Headers.Location!.ToString().ToLowerInvariant());
        });
    }

    [Fact]
    public async Task POST_Roles_Should_Return_400_For_Invalid_Data()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var invalidRequest = new { Name = "", Description = "Invalid role" }; // Empty name

            // Act
            var response = await AuthenticatedPostJsonAsync("/api/roles", invalidRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        });
    }

    [Fact]
    public async Task GET_Roles_Should_Return_All_Roles()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            // Create test roles
            var role1 = TestDataBuilders.CreateRoleRequest("Admin", "Administrator");
            var role2 = TestDataBuilders.CreateRoleRequest("User", "Regular user");

            await AuthenticatedPostJsonAsync("/api/roles", role1);
            await AuthenticatedPostJsonAsync("/api/roles", role2);

            // Act
            var response = await AuthenticatedGetAsync("/api/roles");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var roles = await ReadJsonAsync<List<RoleDto>>(response);

            Assert.NotNull(roles);
            Assert.Equal(2, roles.Count);
            Assert.Contains(roles, r => r.Name == "Admin");
            Assert.Contains(roles, r => r.Name == "User");
        });
    }

    [Fact]
    public async Task GET_Role_By_Id_Should_Return_Role_When_Exists()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var createRequest = TestDataBuilders.CreateRoleRequest("Manager", "Department manager");
            var createResponse = await AuthenticatedPostJsonAsync("/api/roles", createRequest);
            var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

            // Act
            var response = await AuthenticatedGetAsync($"/api/roles/{createdRole!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var role = await ReadJsonAsync<RoleDto>(response);

            Assert.NotNull(role);
            Assert.Equal(createdRole.Id, role!.Id);
            Assert.Equal("Manager", role.Name);
            Assert.Equal("Department manager", role.Description);
        });
    }

    [Fact]
    public async Task GET_Role_By_Id_Should_Return_404_When_Not_Exists()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await AuthenticatedGetAsync($"/api/roles/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    [Fact]
    public async Task PUT_Role_Should_Update_Existing_Role()
    {
        // Arrange
        var createRequest = TestDataBuilders.CreateRoleRequest("Original", "Original description");
        var createResponse = await AuthenticatedPostJsonAsync("/api/roles", createRequest);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        var updateRequest = TestDataBuilders.UpdateRoleRequest("Updated", "Updated description");

        // Act
        var response = await AuthenticatedPutJsonAsync($"/api/roles/{createdRole!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await AuthenticatedGetAsync($"/api/roles/{createdRole.Id}");
        var updatedRole = await ReadJsonAsync<RoleDto>(getResponse);

        Assert.NotNull(updatedRole);
        Assert.Equal("Updated", updatedRole!.Name);
        Assert.Equal("Updated description", updatedRole.Description);
    }

    [Fact]
    public async Task PUT_Role_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdateRoleRequest("Updated", "Updated description");

        // Act
        var response = await AuthenticatedPutJsonAsync($"/api/roles/{nonExistentId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Role_Should_Remove_Existing_Role()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateRoleRequest("ToDelete", "Role to be deleted");
        var createResponse = await AuthenticatedPostJsonAsync("/api/roles", createRequest);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        // Act
        var response = await AuthenticatedDeleteAsync($"/api/roles/{createdRole!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the role is deleted
        var getResponse = await AuthenticatedGetAsync($"/api/roles/{createdRole.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DELETE_Role_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await AuthenticatedDeleteAsync($"/api/roles/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task POST_Role_Should_Handle_Null_Description()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateRoleRequest("SimpleRole", null);

        // Act
        var response = await AuthenticatedPostJsonAsync("/api/roles", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdRole = await ReadJsonAsync<RoleDto>(response);

        Assert.NotNull(createdRole);
        Assert.Equal("SimpleRole", createdRole!.Name);
        Assert.Null(createdRole.Description);
    }

    [Fact]
    public async Task Roles_Should_Persist_Across_Requests()
    {
        // Arrange
        var createRequest = TestDataBuilders.CreateRoleRequest("Persistent", "Should persist");

        // Act & Assert - Create role
        var createResponse = await AuthenticatedPostJsonAsync("/api/roles", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        // Act & Assert - Verify persistence with new request
        var getResponse = await AuthenticatedGetAsync($"/api/roles/{createdRole!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedRole = await ReadJsonAsync<RoleDto>(getResponse);

        Assert.NotNull(retrievedRole);
        Assert.Equal(createdRole.Id, retrievedRole!.Id);
        Assert.Equal("Persistent", retrievedRole.Name);
        Assert.Equal("Should persist", retrievedRole.Description);
    }
}

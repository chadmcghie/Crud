using System.Net;
using System.Net.Http.Json;
using Shared.Dtos;
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
            var response = await Client.GetAsync("/api/roles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var roles = await ReadJsonAsync<List<RoleDto>>(response);
            roles.Should().NotBeNull();
            roles.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task POST_Roles_Should_Create_Role_And_Return_201()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateRoleRequest("Administrator", "System administrator role");

        // Act
        var response = await PostJsonAsync("/api/roles", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdRole = await ReadJsonAsync<RoleDto>(response);
        
        createdRole.Should().NotBeNull();
        createdRole!.Id.Should().NotBeEmpty();
        createdRole.Name.Should().Be("Administrator");
        createdRole.Description.Should().Be("System administrator role");

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain($"/api/roles/{createdRole.Id}".ToLowerInvariant());
    }

    [Fact]
    public async Task POST_Roles_Should_Return_400_For_Invalid_Data()
    {
        // Arrange

        var invalidRequest = new { Name = "", Description = "Invalid role" }; // Empty name

        // Act
        var response = await PostJsonAsync("/api/roles", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Roles_Should_Return_All_Roles()
    {
        // Arrange

        
        // Create test roles
        var role1 = TestDataBuilders.CreateRoleRequest("Admin", "Administrator");
        var role2 = TestDataBuilders.CreateRoleRequest("User", "Regular user");
        
        await PostJsonAsync("/api/roles", role1);
        await PostJsonAsync("/api/roles", role2);

        // Act
        var response = await Client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await ReadJsonAsync<List<RoleDto>>(response);
        
        roles.Should().NotBeNull();
        roles.Should().HaveCount(2);
        roles.Should().Contain(r => r.Name == "Admin");
        roles.Should().Contain(r => r.Name == "User");
    }

    [Fact]
    public async Task GET_Role_By_Id_Should_Return_Role_When_Exists()
    {
        // Arrange
        var createRequest = TestDataBuilders.CreateRoleRequest("Manager", "Department manager");
        var createResponse = await PostJsonAsync("/api/roles", createRequest);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/roles/{createdRole!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var role = await ReadJsonAsync<RoleDto>(response);
        
        role.Should().NotBeNull();
        role!.Id.Should().Be(createdRole.Id);
        role.Name.Should().Be("Manager");
        role.Description.Should().Be("Department manager");
    }

    [Fact]
    public async Task GET_Role_By_Id_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/roles/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Role_Should_Update_Existing_Role()
    {
        // Arrange
        var createRequest = TestDataBuilders.CreateRoleRequest("Original", "Original description");
        var createResponse = await PostJsonAsync("/api/roles", createRequest);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        var updateRequest = TestDataBuilders.UpdateRoleRequest("Updated", "Updated description");

        // Act
        var response = await PutJsonAsync($"/api/roles/{createdRole!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/roles/{createdRole.Id}");
        var updatedRole = await ReadJsonAsync<RoleDto>(getResponse);
        
        updatedRole.Should().NotBeNull();
        updatedRole!.Name.Should().Be("Updated");
        updatedRole.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task PUT_Role_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdateRoleRequest("Updated", "Updated description");

        // Act
        var response = await PutJsonAsync($"/api/roles/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Role_Should_Remove_Existing_Role()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateRoleRequest("ToDelete", "Role to be deleted");
        var createResponse = await PostJsonAsync("/api/roles", createRequest);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/roles/{createdRole!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the role is deleted
        var getResponse = await Client.GetAsync($"/api/roles/{createdRole.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Role_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/roles/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Role_Should_Handle_Null_Description()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateRoleRequest("SimpleRole", null);

        // Act
        var response = await PostJsonAsync("/api/roles", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdRole = await ReadJsonAsync<RoleDto>(response);
        
        createdRole.Should().NotBeNull();
        createdRole!.Name.Should().Be("SimpleRole");
        createdRole.Description.Should().BeNull();
    }

    [Fact]
    public async Task Roles_Should_Persist_Across_Requests()
    {
        // Arrange
        var createRequest = TestDataBuilders.CreateRoleRequest("Persistent", "Should persist");

        // Act & Assert - Create role
        var createResponse = await PostJsonAsync("/api/roles", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdRole = await ReadJsonAsync<RoleDto>(createResponse);

        // Act & Assert - Verify persistence with new request
        var getResponse = await Client.GetAsync($"/api/roles/{createdRole!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedRole = await ReadJsonAsync<RoleDto>(getResponse);
        
        retrievedRole.Should().NotBeNull();
        retrievedRole!.Id.Should().Be(createdRole.Id);
        retrievedRole.Name.Should().Be("Persistent");
        retrievedRole.Description.Should().Be("Should persist");
    }
}

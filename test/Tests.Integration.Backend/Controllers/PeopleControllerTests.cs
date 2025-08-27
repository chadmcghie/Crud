using System.Net;
using System.Net.Http.Json;
using Shared.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

public class PeopleControllerTests : IntegrationTestBase
{
    public PeopleControllerTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_People_Should_Return_Empty_List_Initially()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange


            // Act
            var response = await Client.GetAsync("/api/people");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var people = await ReadJsonAsync<List<PersonResponse>>(response);
            people.Should().NotBeNull();
            people.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task POST_People_Should_Create_Person_And_Return_201()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");

            // Act
            var response = await PostJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);
            
            createdPerson.Should().NotBeNull();
            createdPerson!.Id.Should().NotBeEmpty();
            createdPerson.FullName.Should().Be("John Doe");
            createdPerson.Phone.Should().Be("123-456-7890");
            createdPerson.Roles.Should().BeEmpty();

            // Verify location header
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain($"/api/people/{createdPerson.Id}".ToLowerInvariant());
        });
    }

    [Fact]
    public async Task POST_People_Should_Return_400_For_Invalid_Data()
    {
        // Arrange

        var invalidRequest = new { FullName = "", Phone = "123-456-7890" }; // Empty name

        // Act
        var response = await PostJsonAsync("/api/people", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_People_With_Roles_Should_Create_Person_With_Roles()
    {
        // Arrange
        // Create test roles first
        var role1Request = TestDataBuilders.CreateRoleRequest("Admin", "Administrator");
        var role2Request = TestDataBuilders.CreateRoleRequest("User", "Regular user");
        
        var role1Response = await PostJsonAsync("/api/roles", role1Request);
        var role2Response = await PostJsonAsync("/api/roles", role2Request);
        
        var role1 = await ReadJsonAsync<RoleDto>(role1Response);
        var role2 = await ReadJsonAsync<RoleDto>(role2Response);

        var createRequest = TestDataBuilders.CreatePersonRequest(
            "Jane Smith", 
            "987-654-3210", 
            new[] { role1!.Id, role2!.Id });

        // Act
        var response = await PostJsonAsync("/api/people", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPerson = await ReadJsonAsync<PersonResponse>(response);
        
        createdPerson.Should().NotBeNull();
        createdPerson!.FullName.Should().Be("Jane Smith");
        createdPerson.Roles.Should().HaveCount(2);
        createdPerson.Roles.Should().Contain(r => r.Name == "Admin");
        createdPerson.Roles.Should().Contain(r => r.Name == "User");
    }

    [Fact]
    public async Task POST_People_With_Invalid_Role_Should_Return_400()
    {
        // Arrange

        var nonExistentRoleId = Guid.NewGuid();
        var createRequest = TestDataBuilders.CreatePersonRequest(
            "John Doe", 
            "123-456-7890", 
            new[] { nonExistentRoleId });

        // Act
        var response = await PostJsonAsync("/api/people", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("error");
    }

    [Fact]
    public async Task GET_People_Should_Return_All_People()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange

            
            // Create test people
            var person1 = TestDataBuilders.CreatePersonRequest("Alice Johnson", "111-222-3333");
            var person2 = TestDataBuilders.CreatePersonRequest("Bob Wilson", "444-555-6666");
            
            await PostJsonAsync("/api/people", person1);
            await PostJsonAsync("/api/people", person2);

            // Act
            var response = await Client.GetAsync("/api/people");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var people = await ReadJsonAsync<List<PersonResponse>>(response);
            
            people.Should().NotBeNull();
            people.Should().HaveCount(2);
            people.Should().Contain(p => p.FullName == "Alice Johnson");
            people.Should().Contain(p => p.FullName == "Bob Wilson");
        });
    }

    [Fact]
    public async Task GET_Person_By_Id_Should_Return_Person_When_Exists()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreatePersonRequest("Charlie Brown", "777-888-9999");
        var createResponse = await PostJsonAsync("/api/people", createRequest);
        var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/people/{createdPerson!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var person = await ReadJsonAsync<PersonResponse>(response);
        
        person.Should().NotBeNull();
        person!.Id.Should().Be(createdPerson.Id);
        person.FullName.Should().Be("Charlie Brown");
        person.Phone.Should().Be("777-888-9999");
    }

    [Fact]
    public async Task GET_Person_By_Id_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/people/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Person_Should_Update_Existing_Person()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreatePersonRequest("Original Name", "111-111-1111");
        var createResponse = await PostJsonAsync("/api/people", createRequest);
        var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

        var updateRequest = TestDataBuilders.UpdatePersonRequest("Updated Name", "222-222-2222");

        // Act
        var response = await PutJsonAsync($"/api/people/{createdPerson!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/people/{createdPerson.Id}");
        var updatedPerson = await ReadJsonAsync<PersonResponse>(getResponse);
        
        updatedPerson.Should().NotBeNull();
        updatedPerson!.FullName.Should().Be("Updated Name");
        updatedPerson.Phone.Should().Be("222-222-2222");
    }

    [Fact]
    public async Task PUT_Person_Should_Update_Roles()
    {
        // Arrange

        
        // Create roles
        var role1Response = await PostJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("Admin", "Administrator"));
        var role2Response = await PostJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("User", "Regular user"));
        var role3Response = await PostJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("Manager", "Manager"));
        
        var role1 = await ReadJsonAsync<RoleDto>(role1Response);
        var role2 = await ReadJsonAsync<RoleDto>(role2Response);
        var role3 = await ReadJsonAsync<RoleDto>(role3Response);

        // Create person with initial roles
        var createRequest = TestDataBuilders.CreatePersonRequest("Test User", "123-456-7890", new[] { role1!.Id, role2!.Id });
        var createResponse = await PostJsonAsync("/api/people", createRequest);
        var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

        // Update person with different roles
        var updateRequest = TestDataBuilders.UpdatePersonRequest("Test User", "123-456-7890", new[] { role3!.Id });

        // Act
        var response = await PutJsonAsync($"/api/people/{createdPerson!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the role update
        var getResponse = await Client.GetAsync($"/api/people/{createdPerson.Id}");
        var updatedPerson = await ReadJsonAsync<PersonResponse>(getResponse);
        
        updatedPerson.Should().NotBeNull();
        updatedPerson!.Roles.Should().HaveCount(1);
        updatedPerson.Roles.Should().Contain(r => r.Name == "Manager");
        updatedPerson.Roles.Should().NotContain(r => r.Name == "Admin");
        updatedPerson.Roles.Should().NotContain(r => r.Name == "User");
    }

    [Fact]
    public async Task PUT_Person_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdatePersonRequest("Updated Name", "222-222-2222");

        // Act
        var response = await PutJsonAsync($"/api/people/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Person_Should_Remove_Existing_Person()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreatePersonRequest("To Delete", "999-999-9999");
        var createResponse = await PostJsonAsync("/api/people", createRequest);
        var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/people/{createdPerson!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the person is deleted
        var getResponse = await Client.GetAsync($"/api/people/{createdPerson.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Person_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/people/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Person_Should_Handle_Null_Phone()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreatePersonRequest("No Phone Person", null);

        // Act
        var response = await PostJsonAsync("/api/people", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPerson = await ReadJsonAsync<PersonResponse>(response);
        
        createdPerson.Should().NotBeNull();
        createdPerson!.FullName.Should().Be("No Phone Person");
        createdPerson.Phone.Should().BeNull();
    }

    [Fact]
    public async Task People_Should_Maintain_Role_Relationships()
    {
        // Arrange

        
        // Create a role
        var roleResponse = await PostJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("TestRole", "Test role"));
        var role = await ReadJsonAsync<RoleDto>(roleResponse);

        // Create person with role
        var createRequest = TestDataBuilders.CreatePersonRequest("Role Test", "123-456-7890", new[] { role!.Id });
        var createResponse = await PostJsonAsync("/api/people", createRequest);
        var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

        // Act - Get person and verify role relationship
        var getResponse = await Client.GetAsync($"/api/people/{createdPerson!.Id}");
        var retrievedPerson = await ReadJsonAsync<PersonResponse>(getResponse);

        // Assert
        retrievedPerson.Should().NotBeNull();
        retrievedPerson!.Roles.Should().HaveCount(1);
        var personRole = retrievedPerson.Roles.First();
        personRole.Id.Should().Be(role.Id);
        personRole.Name.Should().Be("TestRole");
        personRole.Description.Should().Be("Test role");
    }
}

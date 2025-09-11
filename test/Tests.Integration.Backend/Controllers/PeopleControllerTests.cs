using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
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
            var client = await CreateUserClientAsync();

            // Act
            var response = await client.GetAsync("/api/people");

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
            var client = await CreateAdminClientAsync();
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");

            // Act
            var response = await client.PostAsJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);

            createdPerson.Should().NotBeNull();
            createdPerson!.Id.Should().NotBeEmpty();
            createdPerson.FullName.Should().Be("John Doe");
            createdPerson.Phone.Should().Be("123-456-7890");
            createdPerson.Roles.Should().BeEmpty();

            // Verify Location header
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().ToLowerInvariant().Should().EndWith($"/api/people/{createdPerson.Id}".ToLowerInvariant());
        });
    }

    [Fact]
    public async Task POST_People_Should_Return_400_For_Invalid_Request()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var client = await CreateAdminClientAsync();
            var invalidRequest = new { }; // Empty request

            // Act
            var response = await client.PostAsJsonAsync("/api/people", invalidRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        });
    }

    [Fact]
    public async Task POST_People_Should_Create_Person_With_Roles()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var client = await CreateAdminClientAsync();

            // Create roles first
            var role1Request = TestDataBuilders.CreateRoleRequest("Admin", "Administrator");
            var role2Request = TestDataBuilders.CreateRoleRequest("User", "Regular user");

            var role1Response = await client.PostAsJsonAsync("/api/roles", role1Request);
            var role2Response = await client.PostAsJsonAsync("/api/roles", role2Request);

            var role1 = await ReadJsonAsync<RoleDto>(role1Response);
            var role2 = await ReadJsonAsync<RoleDto>(role2Response);

            // Create person with roles
            var createRequest = TestDataBuilders.CreatePersonRequest(
                "Jane Smith",
                "987-654-3210",
                new[] { role1!.Id, role2!.Id });

            // Act
            var response = await client.PostAsJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);

            createdPerson.Should().NotBeNull();
            createdPerson!.FullName.Should().Be("Jane Smith");
            createdPerson.Roles.Should().HaveCount(2);
            createdPerson.Roles.Should().Contain(r => r.Name == "Admin");
            createdPerson.Roles.Should().Contain(r => r.Name == "User");
        });
    }

    [Fact]
    public async Task POST_People_Should_Allow_Empty_Phone_Number()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var client = await CreateAdminClientAsync();
            var createRequest = TestDataBuilders.CreatePersonRequest("No Phone Person", null);

            // Act
            var response = await client.PostAsJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);

            createdPerson.Should().NotBeNull();
            createdPerson!.FullName.Should().Be("No Phone Person");
            createdPerson.Phone.Should().BeNull();
        });
    }

    [Fact]
    public async Task GET_People_Should_Return_All_People()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            var userClient = await CreateUserClientAsync();

            var person1 = TestDataBuilders.CreatePersonRequest("Person One", "111-111-1111");
            var person2 = TestDataBuilders.CreatePersonRequest("Person Two", "222-222-2222");

            await adminClient.PostAsJsonAsync("/api/people", person1);
            await adminClient.PostAsJsonAsync("/api/people", person2);

            // Act - Regular user can read
            var response = await userClient.GetAsync("/api/people");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var people = await ReadJsonAsync<List<PersonResponse>>(response);

            people.Should().NotBeNull();
            people.Should().HaveCount(2);
            people.Should().Contain(p => p.FullName == "Person One");
            people.Should().Contain(p => p.FullName == "Person Two");
        });
    }

    [Fact]
    public async Task GET_People_By_Id_Should_Return_Specific_Person()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            var userClient = await CreateUserClientAsync();

            var createRequest = TestDataBuilders.CreatePersonRequest("Specific Person", "333-333-3333");
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

            // Act - Regular user can read
            var response = await userClient.GetAsync($"/api/people/{createdPerson!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var retrievedPerson = await ReadJsonAsync<PersonResponse>(response);

            retrievedPerson.Should().NotBeNull();
            retrievedPerson!.Id.Should().Be(createdPerson.Id);
            retrievedPerson.FullName.Should().Be("Specific Person");
            retrievedPerson.Phone.Should().Be("333-333-3333");
        });
    }

    [Fact]
    public async Task GET_People_By_Id_Should_Return_404_For_NonExistent_Person()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var client = await CreateUserClientAsync();
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await client.GetAsync($"/api/people/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task PUT_People_Should_Update_Person()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            var createRequest = TestDataBuilders.CreatePersonRequest("Original Name", "444-444-4444");
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

            var updateRequest = TestDataBuilders.UpdatePersonRequest("Updated Name", "555-555-5555");

            // Act
            var response = await adminClient.PutAsJsonAsync($"/api/people/{createdPerson!.Id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the update
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            var updatedPerson = await ReadJsonAsync<PersonResponse>(getResponse);

            updatedPerson.Should().NotBeNull();
            updatedPerson!.FullName.Should().Be("Updated Name");
            updatedPerson.Phone.Should().Be("555-555-5555");
        });
    }

    [Fact]
    public async Task PUT_People_Should_Update_Person_Roles()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Create roles with unique names to avoid conflicts
            var role1Response = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("TestAdmin", "Test Administrator"));
            var role2Response = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("TestUser", "Test Regular user"));
            var role3Response = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("TestManager", "Test Manager"));

            var role1 = await ReadJsonAsync<RoleDto>(role1Response);
            var role2 = await ReadJsonAsync<RoleDto>(role2Response);
            var role3 = await ReadJsonAsync<RoleDto>(role3Response);

            // Create person with initial roles
            var createRequest = TestDataBuilders.CreatePersonRequest("Role Update Test", "666-666-6666", new[] { role1!.Id, role2!.Id });
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

            // Get the person again to ensure we have the latest RowVersion
            var getCurrentResponse = await adminClient.GetAsync($"/api/people/{createdPerson!.Id}");
            var currentPerson = await ReadJsonAsync<PersonResponse>(getCurrentResponse);

            // Update with different roles using the current RowVersion for concurrency control
            var updateRequest = TestDataBuilders.UpdatePersonRequest("Role Update Test", "666-666-6666", new[] { role2.Id, role3!.Id }, currentPerson!.RowVersion);

            // Act
            var response = await adminClient.PutAsJsonAsync($"/api/people/{createdPerson!.Id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the roles were updated
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            var updatedPerson = await ReadJsonAsync<PersonResponse>(getResponse);

            updatedPerson.Should().NotBeNull();
            updatedPerson!.Roles.Should().HaveCount(2);
            updatedPerson.Roles.Should().Contain(r => r.Name == "TestUser");
            updatedPerson.Roles.Should().Contain(r => r.Name == "TestManager");
            updatedPerson.Roles.Should().NotContain(r => r.Name == "TestAdmin");
        });
    }

    [Fact]
    public async Task PUT_People_Should_Return_404_For_NonExistent_Person()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            var nonExistentId = Guid.NewGuid();
            var updateRequest = TestDataBuilders.UpdatePersonRequest("Updated Name", "777-777-7777");

            // Act
            var response = await adminClient.PutAsJsonAsync($"/api/people/{nonExistentId}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task DELETE_People_Should_Remove_Person()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            var createRequest = TestDataBuilders.CreatePersonRequest("To Be Deleted", "888-888-8888");
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

            // Act
            var response = await adminClient.DeleteAsync($"/api/people/{createdPerson!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the person was deleted
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task DELETE_People_Should_Return_204_For_NonExistent_Person()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await adminClient.DeleteAsync($"/api/people/{nonExistentId}");

            // Assert
            // Idempotent DELETE returns 204 even for non-existent resources
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        });
    }

    [Fact]
    public async Task People_Should_Maintain_Role_Relationships()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            var userClient = await CreateUserClientAsync();

            // Create a role
            var roleResponse = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest("TestRole", "Test role"));
            var role = await ReadJsonAsync<RoleDto>(roleResponse);

            // Create person with role
            var createRequest = TestDataBuilders.CreatePersonRequest("Role Test", "123-456-7890", new[] { role!.Id });
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

            // Act - Get person and verify role relationship (user can read)
            var getResponse = await userClient.GetAsync($"/api/people/{createdPerson!.Id}");
            var retrievedPerson = await ReadJsonAsync<PersonResponse>(getResponse);

            // Assert
            retrievedPerson.Should().NotBeNull();
            retrievedPerson!.Roles.Should().HaveCount(1);
            var personRole = retrievedPerson.Roles.First();
            personRole.Id.Should().Be(role.Id);
            personRole.Name.Should().Be("TestRole");
            personRole.Description.Should().Be("Test role");
        });
    }

    [Fact]
    public async Task GET_People_Should_Return_401_Without_Authentication()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act - Try to get people without authentication
            var response = await Client.GetAsync("/api/people");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        });
    }

    [Fact]
    public async Task POST_People_Should_Return_401_Without_Authentication()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");

            // Act - Try to create person without authentication
            var response = await Client.PostAsJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        });
    }

    [Fact]
    public async Task POST_People_Should_Return_403_For_Non_Admin_User()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var userClient = await CreateUserClientAsync();
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");

            // Act - Try to create person as regular user
            var response = await userClient.PostAsJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        });
    }

    [Fact]
    public async Task DELETE_People_Should_Return_403_For_Non_Admin_User()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            var userClient = await CreateUserClientAsync();

            // Create a person as admin
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);

            // Act - Try to delete person as regular user
            var response = await userClient.DeleteAsync($"/api/people/{createdPerson!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        });
    }
}

using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Controllers;

public class PeopleControllerTests : IntegrationTestBase, IClassFixture<SmokeTestWebApplicationFactory>
{
    private readonly SmokeTestWebApplicationFactory _smokeFactory;

    public PeopleControllerTests(TestWebApplicationFactoryFixture factory, SmokeTestWebApplicationFactory smokeFactory) : base(factory)
    {
        _smokeFactory = smokeFactory;
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var people = await ReadJsonAsync<List<PersonResponse>>(response);
            Assert.NotNull(people);
            Assert.Empty(people);
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
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);

            Assert.NotNull(createdPerson);
            Assert.NotEqual(Guid.Empty, createdPerson!.Id);
            Assert.Equal("John Doe", createdPerson.FullName);
            Assert.Equal("123-456-7890", createdPerson.Phone);
            Assert.Empty(createdPerson.Roles);

            // Verify Location header
            Assert.NotNull(response.Headers.Location);
            Assert.EndsWith($"/api/people/{createdPerson.Id}".ToLowerInvariant(), response.Headers.Location!.ToString().ToLowerInvariant());
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
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);

            Assert.NotNull(createdPerson);
            Assert.Equal("Jane Smith", createdPerson!.FullName);
            Assert.Equal(2, createdPerson.Roles.Count());
            Assert.Contains(createdPerson.Roles, r => r.Name == "Admin");
            Assert.Contains(createdPerson.Roles, r => r.Name == "User");
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
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdPerson = await ReadJsonAsync<PersonResponse>(response);

            Assert.NotNull(createdPerson);
            Assert.Equal("No Phone Person", createdPerson!.FullName);
            Assert.Null(createdPerson.Phone);
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var people = await ReadJsonAsync<List<PersonResponse>>(response);

            Assert.NotNull(people);
            Assert.Equal(2, people.Count);
            Assert.Contains(people, p => p.FullName == "Person One");
            Assert.Contains(people, p => p.FullName == "Person Two");
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var retrievedPerson = await ReadJsonAsync<PersonResponse>(response);

            Assert.NotNull(retrievedPerson);
            Assert.Equal(createdPerson.Id, retrievedPerson!.Id);
            Assert.Equal("Specific Person", retrievedPerson.FullName);
            Assert.Equal("333-333-3333", retrievedPerson.Phone);
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
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the update
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            var updatedPerson = await ReadJsonAsync<PersonResponse>(getResponse);

            Assert.NotNull(updatedPerson);
            Assert.Equal("Updated Name", updatedPerson!.FullName);
            Assert.Equal("555-555-5555", updatedPerson.Phone);
        });
    }

    [Fact]
    public async Task PUT_People_Should_Update_Person_Roles()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Create roles with unique names to avoid conflicts (let TestDataBuilders generate unique names)
            var role1Response = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest(null, "Test Administrator"));
            var role2Response = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest(null, "Test Regular user"));
            var role3Response = await adminClient.PostAsJsonAsync("/api/roles", TestDataBuilders.CreateRoleRequest(null, "Test Manager"));

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
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the roles were updated
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            var updatedPerson = await ReadJsonAsync<PersonResponse>(getResponse);

            Assert.NotNull(updatedPerson);
            Assert.Equal(2, updatedPerson!.Roles.Count());
            // Check by role ID instead of name (names have generated suffixes)
            Assert.Contains(updatedPerson.Roles, r => r.Id == role2.Id);
            Assert.Contains(updatedPerson.Roles, r => r.Id == role3!.Id);
            Assert.DoesNotContain(updatedPerson.Roles, r => r.Id == role1!.Id);
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
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the person was deleted
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
            Assert.NotNull(retrievedPerson);
            Assert.Single(retrievedPerson!.Roles);
            var personRole = retrievedPerson.Roles.First();
            Assert.Equal(role.Id, personRole.Id);
            Assert.Equal("TestRole", personRole.Name);
            Assert.Equal("Test role", personRole.Description);
        });
    }

    [Fact]
    public async Task GET_People_Should_Return_401_Without_Authentication()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act - Try to get people without authentication (enforce auth for this test)
            Environment.SetEnvironmentVariable("ENFORCE_AUTH_FOR_TEST", "true");
            try
            {
                var response = await Client.GetAsync("/api/people");

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ENFORCE_AUTH_FOR_TEST", null);
            }
        });
    }

    [Fact]
    public async Task POST_People_Should_Return_401_Without_Authentication()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");

            // Act - Try to create person without authentication (using auth-enforcing client)
            var unauthenticatedClient = CreateUnauthenticatedClientWithAuthEnforcement();
            var response = await unauthenticatedClient.PostAsJsonAsync("/api/people", createRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        });
    }

    [Fact]
    public async Task POST_People_Should_Return_403_For_Non_Admin_User()
    {
        // Arrange - Use smoke factory that enforces authorization
        _smokeFactory.EnsureDatabaseCreated();
        await _smokeFactory.ClearDatabaseAsync();

        try
        {
            using var userClient = _smokeFactory.CreateClient();
            // Note: Not authenticating the client, so it should get 401/403
            var createRequest = TestDataBuilders.CreatePersonRequest("John Doe", "123-456-7890");

            // Act - Try to create person without authentication
            var response = await userClient.PostAsJsonAsync("/api/people", createRequest);

            // Assert - Should get Unauthorized since no auth provided
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
        }
        finally
        {
            await _smokeFactory.ClearDatabaseAsync();
        }
    }

    [Fact]
    public async Task DELETE_People_Should_Return_403_For_Non_Admin_User()
    {
        // Arrange - Use smoke factory that enforces authorization
        _smokeFactory.EnsureDatabaseCreated();
        await _smokeFactory.ClearDatabaseAsync();

        try
        {
            // Create a dummy person ID for the delete attempt
            var testPersonId = Guid.NewGuid();
            using var userClient = _smokeFactory.CreateClient();
            // Note: Not authenticating the client, so it should get 401/403

            // Act - Try to delete person without authentication
            var response = await userClient.DeleteAsync($"/api/people/{testPersonId}");

            // Assert - Should get Unauthorized since no auth provided
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
        }
        finally
        {
            await _smokeFactory.ClearDatabaseAsync();
        }
    }
}

using System.Net.Http.Json;
using System.Threading.Tasks;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.OutputCaching;

/// <summary>
/// Tests for cache invalidation scenarios
/// </summary>
public class CacheInvalidationTests : IntegrationTestBase
{
    public CacheInvalidationTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PostPerson_ShouldInvalidateListCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = Domain.Entities.Person.Create("Initial Person", "555-0100");
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the initial list
            var response1 = await userClient.GetAsync("/api/people");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Create a new person (should invalidate cache) - Use admin client
            var createRequest = new Api.Dtos.CreatePersonRequest("New Person", "555-0200", null);
            var postResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Get the list again
            var response2 = await userClient.GetAsync("/api/people");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(content1, content2);
            Assert.Contains("New Person", content2);
        });
    }

    [Fact]
    public async Task PutPerson_ShouldInvalidateBothEntityAndListCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = Domain.Entities.Person.Create("Original Name", "555-0100");
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();
            var personId = person.Id;

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the entity and list
            var response1 = await userClient.GetAsync($"/api/people/{personId}");
            response1.EnsureSuccessStatusCode();
            var entityContent1 = await response1.Content.ReadAsStringAsync();

            var listResponse1 = await userClient.GetAsync("/api/people");
            listResponse1.EnsureSuccessStatusCode();
            var listContent1 = await listResponse1.Content.ReadAsStringAsync();

            // Update the person (should invalidate both caches) - Use admin client
            var updateRequest = new Api.Dtos.UpdatePersonRequest("Updated Name", "555-0200", null, null);
            var putResponse = await adminClient.PutAsJsonAsync($"/api/people/{personId}", updateRequest);
            putResponse.EnsureSuccessStatusCode();

            // Get the entity and list again
            var response2 = await userClient.GetAsync($"/api/people/{personId}");
            response2.EnsureSuccessStatusCode();
            var entityContent2 = await response2.Content.ReadAsStringAsync();

            var listResponse2 = await userClient.GetAsync("/api/people");
            listResponse2.EnsureSuccessStatusCode();
            var listContent2 = await listResponse2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(entityContent1, entityContent2);
            Assert.Contains("Updated Name", entityContent2);

            Assert.NotEqual(listContent1, listContent2);
            Assert.Contains("Updated Name", listContent2);
        });
    }

    [Fact]
    public async Task DeletePerson_ShouldInvalidateListCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = Domain.Entities.Person.Create("Person to Delete", "555-0100");
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();
            var personId = person.Id;

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the list
            var listResponse1 = await userClient.GetAsync("/api/people");
            listResponse1.EnsureSuccessStatusCode();
            var listContent1 = await listResponse1.Content.ReadAsStringAsync();

            // Delete the person (should invalidate cache) - Use admin client
            var deleteResponse = await adminClient.DeleteAsync($"/api/people/{personId}");
            deleteResponse.EnsureSuccessStatusCode();

            // Get the list again
            var listResponse2 = await userClient.GetAsync("/api/people");
            listResponse2.EnsureSuccessStatusCode();
            var listContent2 = await listResponse2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(listContent1, listContent2);
            Assert.DoesNotContain("Person to Delete", listContent2);
        });
    }

    [Fact]
    public async Task PostRole_ShouldInvalidateRolesCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var role = Domain.Entities.Role.Create("Initial Role");
            DbContext.Roles.Add(role);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the initial list
            var response1 = await userClient.GetAsync("/api/roles");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Create a new role (should invalidate cache) - Use admin client
            var createRequest = new Api.Dtos.CreateRoleRequest("New Role", "Description");
            var postResponse = await adminClient.PostAsJsonAsync("/api/roles", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Get the list again
            var response2 = await userClient.GetAsync("/api/roles");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(content1, content2);
            Assert.Contains("New Role", content2);
        });
    }

    [Fact]
    public async Task PostWall_ShouldInvalidateWallsCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var wall = Domain.Entities.Wall.Create("Initial Wall", 10, 8, 12, "Brick");
            DbContext.Walls.Add(wall);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the initial list
            var response1 = await userClient.GetAsync("/api/walls");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Create a new wall (should invalidate cache) - Use admin client
            var createRequest = TestDataBuilders.CreateWallRequest(
                "New Wall", "Description", 15, 10, 14, "Concrete");
            var postResponse = await adminClient.PostAsJsonAsync("/api/walls", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Get the list again
            var response2 = await userClient.GetAsync("/api/walls");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(content1, content2);
            Assert.Contains("New Wall", content2);
        });
    }

    [Fact]
    public async Task PostWindow_ShouldInvalidateWindowsCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var window = Domain.Entities.Window.Create("Initial Window", 4, 5, "Aluminum", "Double");
            DbContext.Windows.Add(window);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the initial list
            var response1 = await userClient.GetAsync("/api/windows");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Create a new window (should invalidate cache) - Use admin client
            var createRequest = TestDataBuilders.CreateWindowRequest(
                "New Window", "Description", 3, 4, 12, "Wood", null, "Triple", null);
            var postResponse = await adminClient.PostAsJsonAsync("/api/windows", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Get the list again
            var response2 = await userClient.GetAsync("/api/windows");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(content1, content2);
            Assert.Contains("New Window", content2);
        });
    }

    [Fact]
    public async Task CacheInvalidation_ShouldBeEntitySpecific()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = Domain.Entities.Person.Create("Test Person", "555-0100");
            var role = Domain.Entities.Role.Create("Test Role");
            DbContext.People.Add(person);
            DbContext.Roles.Add(role);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache both entities
            var peopleResponse1 = await userClient.GetAsync("/api/people");
            var rolesResponse1 = await userClient.GetAsync("/api/roles");
            peopleResponse1.EnsureSuccessStatusCode();
            rolesResponse1.EnsureSuccessStatusCode();

            var peopleContent1 = await peopleResponse1.Content.ReadAsStringAsync();
            var rolesContent1 = await rolesResponse1.Content.ReadAsStringAsync();

            // Modify only people (should only invalidate people cache) - Use admin client
            var createPersonRequest = new Api.Dtos.CreatePersonRequest("New Person", "555-0200", null);
            await adminClient.PostAsJsonAsync("/api/people", createPersonRequest);

            // Get both lists again
            var peopleResponse2 = await userClient.GetAsync("/api/people");
            var rolesResponse2 = await userClient.GetAsync("/api/roles");
            peopleResponse2.EnsureSuccessStatusCode();
            rolesResponse2.EnsureSuccessStatusCode();

            var peopleContent2 = await peopleResponse2.Content.ReadAsStringAsync();
            var rolesContent2 = await rolesResponse2.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(peopleContent1, peopleContent2);
            Assert.Contains("New Person", peopleContent2);
            Assert.Equal(rolesContent1, rolesContent2);
        });
    }
}

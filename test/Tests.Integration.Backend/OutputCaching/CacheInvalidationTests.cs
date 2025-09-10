using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
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
            var person = new Domain.Entities.Person { FullName = "Initial Person", Phone = "555-0100" };
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
            content2.Should().NotBe(content1, "Cache should be invalidated after POST");
            content2.Should().Contain("New Person");
        });
    }

    [Fact]
    public async Task PutPerson_ShouldInvalidateBothEntityAndListCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Original Name", Phone = "555-0100" };
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
            var updateRequest = new Api.Dtos.UpdatePersonRequest("Updated Name", "555-0200", null);
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
            entityContent2.Should().NotBe(entityContent1, "Entity cache should be invalidated after PUT");
            entityContent2.Should().Contain("Updated Name");

            listContent2.Should().NotBe(listContent1, "List cache should be invalidated after PUT");
            listContent2.Should().Contain("Updated Name");
        });
    }

    [Fact]
    public async Task DeletePerson_ShouldInvalidateListCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Person to Delete", Phone = "555-0100" };
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
            listContent2.Should().NotBe(listContent1, "List cache should be invalidated after DELETE");
            listContent2.Should().NotContain("Person to Delete");
        });
    }

    [Fact]
    public async Task PostRole_ShouldInvalidateRolesCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var role = new Domain.Entities.Role { Name = "Initial Role" };
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
            content2.Should().NotBe(content1, "Cache should be invalidated after POST");
            content2.Should().Contain("New Role");
        });
    }

    [Fact]
    public async Task PostWall_ShouldInvalidateWallsCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var wall = new Domain.Entities.Wall
            {
                Name = "Initial Wall",
                AssemblyType = "Brick",
                Length = 10,
                Height = 8,
                Thickness = 12
            };
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
            content2.Should().NotBe(content1, "Cache should be invalidated after POST");
            content2.Should().Contain("New Wall");
        });
    }

    [Fact]
    public async Task PostWindow_ShouldInvalidateWindowsCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var window = new Domain.Entities.Window
            {
                Name = "Initial Window",
                FrameType = "Aluminum",
                GlazingType = "Double",
                Width = 4,
                Height = 5,
                Area = 20
            };
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
            content2.Should().NotBe(content1, "Cache should be invalidated after POST");
            content2.Should().Contain("New Window");
        });
    }

    [Fact]
    public async Task CacheInvalidation_ShouldBeEntitySpecific()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            var role = new Domain.Entities.Role { Name = "Test Role" };
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
            peopleContent2.Should().NotBe(peopleContent1, "People cache should be invalidated");
            peopleContent2.Should().Contain("New Person");
            rolesContent2.Should().Be(rolesContent1, "Roles cache should NOT be invalidated");
        });
    }
}

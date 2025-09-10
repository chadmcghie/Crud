using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.OutputCaching;

/// <summary>
/// Tests for cache invalidation when data is modified
/// </summary>
public class CacheInvalidationTests : IntegrationTestBase
{
    public CacheInvalidationTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PostPerson_ShouldInvalidateCollectionCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create initial data
            var person1 = new Domain.Entities.Person { FullName = "Person 1", Phone = "555-0001" };
            DbContext.People.Add(person1);
            await DbContext.SaveChangesAsync();

            // Act - Get list (should cache)
            var response1 = await Client.GetAsync("/api/people");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Create new person (should invalidate cache)
            var createRequest = new Api.Dtos.CreatePersonRequest("Person Two", "555-0002", null);
            var postResponse = await Client.PostAsJsonAsync("/api/people", createRequest);
            if (!postResponse.IsSuccessStatusCode)
            {
                var error = await postResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create person: {postResponse.StatusCode} - {error}");
            }
            postResponse.EnsureSuccessStatusCode();

            // Act - Get list again (should have new data)
            var response2 = await Client.GetAsync("/api/people");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().NotBe(content2, "Cache should be invalidated after POST");
            content2.Should().Contain("Person Two", "New person should be in the response");
        });
    }

    [Fact]
    public async Task PutPerson_ShouldInvalidateBothEntityAndCollectionCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create initial data
            var person = new Domain.Entities.Person { FullName = "Original Name", Phone = "555-0001" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();
            var personId = person.Id;

            // Act - Get entity (should cache)
            var response1 = await Client.GetAsync($"/api/people/{personId}");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Get list (should cache)
            var listResponse1 = await Client.GetAsync("/api/people");
            listResponse1.EnsureSuccessStatusCode();
            var listContent1 = await listResponse1.Content.ReadAsStringAsync();

            // Act - Update person (should invalidate both caches)
            var updateRequest = new Api.Dtos.UpdatePersonRequest("Updated Name", "555-9999", null);
            var putResponse = await Client.PutAsJsonAsync($"/api/people/{personId}", updateRequest);
            putResponse.EnsureSuccessStatusCode();

            // Act - Get entity again (should have new data)
            var response2 = await Client.GetAsync($"/api/people/{personId}");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Act - Get list again (should have updated data)
            var listResponse2 = await Client.GetAsync("/api/people");
            listResponse2.EnsureSuccessStatusCode();
            var listContent2 = await listResponse2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().NotBe(content2, "Entity cache should be invalidated after PUT");
            content2.Should().Contain("Updated Name", "Entity should have updated name");

            listContent1.Should().NotBe(listContent2, "Collection cache should be invalidated after PUT");
            listContent2.Should().Contain("Updated Name", "Collection should have updated name");
        });
    }

    [Fact]
    public async Task DeletePerson_ShouldInvalidateBothEntityAndCollectionCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create initial data
            var person1 = new Domain.Entities.Person { FullName = "Person One", Phone = "555-0001" };
            var person2 = new Domain.Entities.Person { FullName = "Person Two", Phone = "555-0002" };
            DbContext.People.AddRange(person1, person2);
            await DbContext.SaveChangesAsync();
            var personId = person2.Id;

            // Act - Get list (should cache)
            var listResponse1 = await Client.GetAsync("/api/people");
            listResponse1.EnsureSuccessStatusCode();
            var listContent1 = await listResponse1.Content.ReadAsStringAsync();

            // Act - Delete person (should invalidate cache)
            var deleteResponse = await Client.DeleteAsync($"/api/people/{personId}");
            deleteResponse.EnsureSuccessStatusCode();

            // Act - Get list again (should have one less person)
            var listResponse2 = await Client.GetAsync("/api/people");
            listResponse2.EnsureSuccessStatusCode();
            var listContent2 = await listResponse2.Content.ReadAsStringAsync();

            // Assert
            listContent1.Should().NotBe(listContent2, "Collection cache should be invalidated after DELETE");
            listContent2.Should().NotContain("Person Two", "Deleted person should not be in the response");
            listContent2.Should().Contain("Person One", "Remaining person should still be in the response");
        });
    }

    [Fact]
    public async Task PostRole_ShouldInvalidateRolesCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create initial data
            var role1 = new Domain.Entities.Role { Name = "Admin" };
            DbContext.Roles.Add(role1);
            await DbContext.SaveChangesAsync();

            // Act - Get list (should cache)
            var response1 = await Client.GetAsync("/api/roles");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Create new role (should invalidate cache)
            var createRequest = new { name = "Manager", description = "Manager role" };
            var postResponse = await Client.PostAsJsonAsync("/api/roles", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Act - Get list again (should have new data)
            var response2 = await Client.GetAsync("/api/roles");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().NotBe(content2, "Cache should be invalidated after POST");
            content2.Should().Contain("Manager", "New role should be in the response");
        });
    }

    [Fact]
    public async Task PostWall_ShouldInvalidateWallsCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create initial data
            var wall1 = new Domain.Entities.Wall
            {
                Name = "Wall 1",
                AssemblyType = "Brick",
                Length = 10,
                Height = 8,
                Thickness = 12
            };
            DbContext.Walls.Add(wall1);
            await DbContext.SaveChangesAsync();

            // Act - Get list (should cache)
            var response1 = await Client.GetAsync("/api/walls");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Create new wall (should invalidate cache)
            var createRequest = new
            {
                name = "Wall Two",
                assemblyType = "Concrete",
                length = 15.0,
                height = 10.0,
                thickness = 8.0
            };
            var postResponse = await Client.PostAsJsonAsync("/api/walls", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Act - Get list again (should have new data)
            var response2 = await Client.GetAsync("/api/walls");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().NotBe(content2, "Cache should be invalidated after POST");
            content2.Should().Contain("Wall Two", "New wall should be in the response");
        });
    }

    [Fact]
    public async Task PostWindow_ShouldInvalidateWindowsCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create initial data
            var window1 = new Domain.Entities.Window
            {
                Name = "Window 1",
                FrameType = "Aluminum",
                GlazingType = "Double",
                Width = 4,
                Height = 5,
                Area = 20
            };
            DbContext.Windows.Add(window1);
            await DbContext.SaveChangesAsync();

            // Act - Get list (should cache)
            var response1 = await Client.GetAsync("/api/windows");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Create new window (should invalidate cache)
            var createRequest = new
            {
                name = "Window Two",
                frameType = "Vinyl",
                glazingType = "Triple",
                width = 3.0,
                height = 4.0,
                area = 12.0
            };
            var postResponse = await Client.PostAsJsonAsync("/api/windows", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Act - Get list again (should have new data)
            var response2 = await Client.GetAsync("/api/windows");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().NotBe(content2, "Cache should be invalidated after POST");
            content2.Should().Contain("Window Two", "New window should be in the response");
        });
    }
}

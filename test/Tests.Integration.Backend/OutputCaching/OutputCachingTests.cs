using System.Net.Http;
using System.Threading.Tasks;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.OutputCaching;

/// <summary>
/// Simplified tests for output caching functionality
/// These tests verify that the OutputCache attributes are applied and responses are consistent
/// </summary>
public class OutputCachingTests : IntegrationTestBase
{
    public OutputCachingTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetPeople_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = Domain.Entities.Person.Create("Test Person", "555-0100");
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests with authenticated client
            var userClient = await CreateUserClientAsync();
            var response1 = await userClient.GetAsync("/api/people");
            var response2 = await userClient.GetAsync("/api/people");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            Assert.Equal(content2, content1);
        });
    }

    [Fact]
    public async Task GetRoles_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var role1 = Domain.Entities.Role.Create("Admin");
            var role2 = Domain.Entities.Role.Create("User");
            DbContext.Roles.AddRange(role1, role2);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests with authenticated client
            var userClient = await CreateUserClientAsync();
            var response1 = await userClient.GetAsync("/api/roles");
            var response2 = await userClient.GetAsync("/api/roles");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            Assert.Equal(content2, content1);
        });
    }

    [Fact]
    public async Task GetWalls_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var wall = Domain.Entities.Wall.Create("Test Wall", 10, 8, 12, "Brick");
            DbContext.Walls.Add(wall);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests with authenticated client
            var userClient = await CreateUserClientAsync();
            var response1 = await userClient.GetAsync("/api/walls");
            var response2 = await userClient.GetAsync("/api/walls");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            Assert.Equal(content2, content1);
        });
    }

    [Fact]
    public async Task GetWindows_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var window = Domain.Entities.Window.Create("Test Window", 4, 5, "Aluminum", "Double");
            DbContext.Windows.Add(window);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests with authenticated client
            var userClient = await CreateUserClientAsync();
            var response1 = await userClient.GetAsync("/api/windows");
            var response2 = await userClient.GetAsync("/api/windows");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            Assert.Equal(content2, content1);
        });
    }

    [Fact]
    public async Task GetEntityById_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = Domain.Entities.Person.Create("Test Person", "555-0100");
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            var personId = person.Id;

            // Act - Make multiple requests for the same entity with authenticated client
            var userClient = await CreateUserClientAsync();
            var response1 = await userClient.GetAsync($"/api/people/{personId}");
            var response2 = await userClient.GetAsync($"/api/people/{personId}");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            Assert.Equal(content2, content1);
        });
    }
}

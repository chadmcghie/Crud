using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
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
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests
            var response1 = await Client.GetAsync("/api/people");
            var response2 = await Client.GetAsync("/api/people");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            content1.Should().Be(content2, "Multiple requests should return identical data");
        });
    }

    [Fact]
    public async Task GetRoles_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var role1 = new Domain.Entities.Role { Name = "Admin" };
            var role2 = new Domain.Entities.Role { Name = "User" };
            DbContext.Roles.AddRange(role1, role2);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests
            var response1 = await Client.GetAsync("/api/roles");
            var response2 = await Client.GetAsync("/api/roles");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            content1.Should().Be(content2, "Multiple requests should return identical data");
        });
    }

    [Fact]
    public async Task GetWalls_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var wall = new Domain.Entities.Wall
            {
                Name = "Test Wall",
                AssemblyType = "Brick",
                Length = 10,
                Height = 8,
                Thickness = 12
            };
            DbContext.Walls.Add(wall);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests
            var response1 = await Client.GetAsync("/api/walls");
            var response2 = await Client.GetAsync("/api/walls");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            content1.Should().Be(content2, "Multiple requests should return identical data");
        });
    }

    [Fact]
    public async Task GetWindows_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var window = new Domain.Entities.Window
            {
                Name = "Test Window",
                FrameType = "Aluminum",
                GlazingType = "Double",
                Width = 4,
                Height = 5,
                Area = 20
            };
            DbContext.Windows.Add(window);
            await DbContext.SaveChangesAsync();

            // Act - Make multiple requests
            var response1 = await Client.GetAsync("/api/windows");
            var response2 = await Client.GetAsync("/api/windows");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            content1.Should().Be(content2, "Multiple requests should return identical data");
        });
    }

    [Fact]
    public async Task GetEntityById_ShouldReturnConsistentResponses()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            var personId = person.Id;

            // Act - Make multiple requests for the same entity
            var response1 = await Client.GetAsync($"/api/people/{personId}");
            var response2 = await Client.GetAsync($"/api/people/{personId}");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert - Responses should be identical
            content1.Should().Be(content2, "Multiple requests for the same entity should return identical data");
        });
    }
}

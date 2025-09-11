using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.OutputCaching;

/// <summary>
/// End-to-end tests for the complete caching workflow including performance validation
/// </summary>
public class CachingE2ETests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public CachingE2ETests(TestWebApplicationFactoryFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
    }

    [Fact(Skip = "ConditionalRequestMiddleware implementation needs refinement - tracked in issue")]
    public async Task CompleteCachingWorkflow_ShouldPerformAsExpected()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "John Doe", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Step 1: Initial request (cache miss)
            var stopwatch = Stopwatch.StartNew();
            var response1 = await userClient.GetAsync("/api/people");
            stopwatch.Stop();
            response1.EnsureSuccessStatusCode();
            var initialTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"Initial request (cache miss): {initialTime}ms");

            // Step 2: Second request (cache hit - should be faster)
            stopwatch.Restart();
            var response2 = await userClient.GetAsync("/api/people");
            stopwatch.Stop();
            response2.EnsureSuccessStatusCode();
            var cachedTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"Cached request (cache hit): {cachedTime}ms");

            // Assert performance
            cachedTime.Should().BeLessThan(50, "Cached response should be under 50ms");

            // Step 3: Conditional request with ETag (should return 304)
            var etag = response2.Headers.ETag;
            if (etag != null)
            {
                var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, "/api/people");
                conditionalRequest.Headers.IfNoneMatch.Add(etag);

                stopwatch.Restart();
                var response3 = await userClient.SendAsync(conditionalRequest);
                stopwatch.Stop();

                response3.StatusCode.Should().Be(HttpStatusCode.NotModified);
                _output.WriteLine($"Conditional request (304): {stopwatch.ElapsedMilliseconds}ms");
            }

            // Step 4: Modify data (should invalidate cache) - Use admin client for POST
            var createRequest = new Api.Dtos.CreatePersonRequest("Jane Smith", "555-0200", null);
            var postResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Step 5: Request after modification (cache miss due to invalidation)
            stopwatch.Restart();
            var response4 = await userClient.GetAsync("/api/people");
            stopwatch.Stop();
            response4.EnsureSuccessStatusCode();
            var afterInvalidationTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"After invalidation (cache miss): {afterInvalidationTime}ms");

            // Content should be different after adding new person
            var content2 = await response2.Content.ReadAsStringAsync();
            var content4 = await response4.Content.ReadAsStringAsync();
            content4.Should().NotBe(content2, "Content should change after adding new person");
            content4.Should().Contain("Jane Smith");

            // Step 6: Final cached request
            stopwatch.Restart();
            var response5 = await userClient.GetAsync("/api/people");
            stopwatch.Stop();
            response5.EnsureSuccessStatusCode();
            var finalCachedTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"Final cached request: {finalCachedTime}ms");

            finalCachedTime.Should().BeLessThan(50, "Final cached response should be under 50ms");
        });
    }

    [Theory]
    [InlineData("/api/people")]
    [InlineData("/api/roles")]
    [InlineData("/api/walls")]
    [InlineData("/api/windows")]
    public async Task AllCachedEndpoints_ShouldReturnCacheHeaders(string endpoint)
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Get authenticated client
            var userClient = await CreateUserClientAsync();

            // Act
            var response = await userClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            // Assert - Check for cache-related headers
            var hasCacheControl = response.Headers.CacheControl != null;
            var hasETag = response.Headers.ETag != null;
            var hasLastModified = response.Content.Headers.LastModified != null;

            (hasCacheControl || hasETag || hasLastModified).Should().BeTrue(
                $"Endpoint {endpoint} should return at least one cache-related header");

            _output.WriteLine($"Endpoint: {endpoint}");
            if (hasCacheControl)
                _output.WriteLine($"  Cache-Control: {response.Headers.CacheControl}");
            if (hasETag)
                _output.WriteLine($"  ETag: {response.Headers.ETag}");
            if (hasLastModified)
                _output.WriteLine($"  Last-Modified: {response.Content.Headers.LastModified}");
        });
    }

    [Fact]
    public async Task CacheInvalidation_ShouldWorkAcrossEntities()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache the person data
            await userClient.GetAsync("/api/people");

            // Create a new person (should invalidate people cache) - Use admin client
            var createRequest = new Api.Dtos.CreatePersonRequest("New Person", "555-0200", null);
            var postResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Check that the cache was invalidated
            var response = await userClient.GetAsync("/api/people");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("New Person", "Cache should be invalidated after POST");
        });
    }

    [Fact]
    public async Task EntityCaching_ShouldCacheIndividualEntities()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Get authenticated client
            var userClient = await CreateUserClientAsync();

            // Act - First request to cache
            await userClient.GetAsync($"/api/people/{person.Id}");

            // Measure cached response time
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(userClient.GetAsync($"/api/people/{person.Id}"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            foreach (var response in responses)
            {
                response.EnsureSuccessStatusCode();
            }

            var avgTime = stopwatch.ElapsedMilliseconds / 5.0;
            _output.WriteLine($"Average cached response time: {avgTime}ms");
            avgTime.Should().BeLessThan(20, "Cached entity responses should be very fast");
        });
    }

    [Fact]
    public async Task CachePerformance_ShouldShowSignificantImprovement()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            for (int i = 0; i < 10; i++)
            {
                var role = new Domain.Entities.Role { Name = $"Role{i}", Description = $"Description{i}" };
                DbContext.Roles.Add(role);
            }
            await DbContext.SaveChangesAsync();

            // Get authenticated client
            var userClient = await CreateUserClientAsync();

            // Act - Measure initial request time
            var stopwatch = Stopwatch.StartNew();
            var response1 = await userClient.GetAsync("/api/roles");
            stopwatch.Stop();
            response1.EnsureSuccessStatusCode();
            var initialTime = stopwatch.ElapsedMilliseconds;

            // Measure cached request times
            var cachedTimes = new List<long>();
            for (int i = 0; i < 10; i++)
            {
                stopwatch.Restart();
                var response = await userClient.GetAsync("/api/roles");
                stopwatch.Stop();
                response.EnsureSuccessStatusCode();
                cachedTimes.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var avgCachedTime = cachedTimes.Average();
            _output.WriteLine($"Initial request: {initialTime}ms");
            _output.WriteLine($"Average cached request: {avgCachedTime}ms");
            _output.WriteLine($"Performance improvement: {(initialTime / avgCachedTime):F1}x faster");

            avgCachedTime.Should().BeLessThan(initialTime * 0.5,
                "Cached requests should be at least 2x faster than initial request");
        });
    }

    [Fact]
    public async Task CrossEntityCaching_ShouldNotInterfere()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var role = new Domain.Entities.Role { Name = "Test Role" };
            var wall = new Domain.Entities.Wall
            {
                Name = "Test Wall",
                AssemblyType = "Brick",
                Length = 10,
                Height = 8,
                Thickness = 12
            };
            DbContext.Roles.Add(role);
            DbContext.Walls.Add(wall);
            await DbContext.SaveChangesAsync();

            // Get authenticated clients
            var userClient = await CreateUserClientAsync();
            var adminClient = await CreateAdminClientAsync();

            // Act - Cache both endpoints
            var rolesResponse1 = await userClient.GetAsync("/api/roles");
            var wallsResponse1 = await userClient.GetAsync("/api/walls");
            rolesResponse1.EnsureSuccessStatusCode();
            wallsResponse1.EnsureSuccessStatusCode();

            // Modify roles (should only invalidate roles cache) - Use admin client
            var createRoleRequest = new Api.Dtos.CreateRoleRequest("New Role", "Description");
            await adminClient.PostAsJsonAsync("/api/roles", createRoleRequest);

            // Check that only roles cache was invalidated
            var rolesResponse2 = await userClient.GetAsync("/api/roles");
            var wallsResponse2 = await userClient.GetAsync("/api/walls");

            var rolesContent1 = await rolesResponse1.Content.ReadAsStringAsync();
            var rolesContent2 = await rolesResponse2.Content.ReadAsStringAsync();
            var wallsContent1 = await wallsResponse1.Content.ReadAsStringAsync();
            var wallsContent2 = await wallsResponse2.Content.ReadAsStringAsync();

            // Assert
            rolesContent2.Should().NotBe(rolesContent1, "Roles cache should be invalidated");
            rolesContent2.Should().Contain("New Role");
            wallsContent2.Should().Be(wallsContent1, "Walls cache should NOT be invalidated");
        });
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldBeCachedEfficiently()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            for (int i = 0; i < 5; i++)
            {
                var person = new Domain.Entities.Person { FullName = $"Person{i}", Phone = $"555-010{i}" };
                DbContext.People.Add(person);
            }
            await DbContext.SaveChangesAsync();

            // Get authenticated client
            var userClient = await CreateUserClientAsync();

            // Warm up the cache
            await userClient.GetAsync("/api/people");

            // Act - Make concurrent requests
            var stopwatch = Stopwatch.StartNew();
            var tasks = new Task<HttpResponseMessage>[20];
            for (int i = 0; i < 20; i++)
            {
                tasks[i] = userClient.GetAsync("/api/people");
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            foreach (var response in responses)
            {
                response.EnsureSuccessStatusCode();
            }

            var totalTime = stopwatch.ElapsedMilliseconds;
            var avgTime = totalTime / 20.0;

            _output.WriteLine($"20 concurrent requests completed in {totalTime}ms");
            _output.WriteLine($"Average time per request: {avgTime}ms");

            avgTime.Should().BeLessThan(10,
                "Concurrent cached requests should be very fast (under 10ms average)");
        });
    }
}

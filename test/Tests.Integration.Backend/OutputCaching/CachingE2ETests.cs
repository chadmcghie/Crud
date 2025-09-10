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

    [Fact]
    public async Task CompleteCachingWorkflow_ShouldPerformAsExpected()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "John Doe", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Step 1: Initial request (cache miss)
            var stopwatch = Stopwatch.StartNew();
            var response1 = await Client.GetAsync("/api/people");
            stopwatch.Stop();
            response1.EnsureSuccessStatusCode();
            var initialTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"Initial request (cache miss): {initialTime}ms");

            // Step 2: Second request (cache hit - should be faster)
            stopwatch.Restart();
            var response2 = await Client.GetAsync("/api/people");
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
                var response3 = await Client.SendAsync(conditionalRequest);
                stopwatch.Stop();
                
                response3.StatusCode.Should().Be(HttpStatusCode.NotModified);
                _output.WriteLine($"Conditional request (304): {stopwatch.ElapsedMilliseconds}ms");
            }

            // Step 4: Modify data (should invalidate cache)
            var createRequest = new Api.Dtos.CreatePersonRequest("Jane Smith", "555-0200", null);
            var postResponse = await Client.PostAsJsonAsync("/api/people", createRequest);
            postResponse.EnsureSuccessStatusCode();

            // Step 5: Request after modification (cache miss due to invalidation)
            stopwatch.Restart();
            var response4 = await Client.GetAsync("/api/people");
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
            var response5 = await Client.GetAsync("/api/people");
            stopwatch.Stop();
            response5.EnsureSuccessStatusCode();
            var finalCachedTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"Final cached request: {finalCachedTime}ms");
            
            finalCachedTime.Should().BeLessThan(50, "Final cached response should be under 50ms");
        });
    }

    [Fact]
    public async Task CacheHeaders_ShouldBePresentOnAllGetEndpoints()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Test various endpoints for proper cache headers
            var endpoints = new[]
            {
                "/api/people",
                "/api/roles",
                "/api/walls",
                "/api/windows"
            };

            foreach (var endpoint in endpoints)
            {
                var response = await Client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                // Check for cache-related headers
                response.Headers.Should().ContainKey("ETag", 
                    $"{endpoint} should have ETag header");
                
                // Last-Modified is a content header (may be null for empty collections)
                var lastModified = response.Content.Headers.LastModified;
                    
                _output.WriteLine($"{endpoint}: ETag={response.Headers.ETag}, " +
                    $"Last-Modified={lastModified?.ToString() ?? "(not set)"}");
            }
        });
    }

    [Fact]
    public async Task CacheInvalidation_ShouldCompleteQuickly()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Warm up the cache
            await Client.GetAsync("/api/people");

            // Act - Measure invalidation time
            var createRequest = new Api.Dtos.CreatePersonRequest("Test Person", "555-0300", null);
            
            var stopwatch = Stopwatch.StartNew();
            var postResponse = await Client.PostAsJsonAsync("/api/people", createRequest);
            stopwatch.Stop();
            
            postResponse.EnsureSuccessStatusCode();
            
            // Assert - Invalidation shouldn't significantly slow down the operation
            _output.WriteLine($"POST with cache invalidation: {stopwatch.ElapsedMilliseconds}ms");
            
            // The POST operation including cache invalidation should still be reasonably fast
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, 
                "POST with cache invalidation should complete within 200ms");
        });
    }

    [Fact]
    public async Task CacheHitRatio_ShouldBeHighUnderLoad()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Cache Test", Phone = "555-0400" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Warm up cache
            await Client.GetAsync($"/api/people/{person.Id}");

            // Act - Make multiple requests to simulate load
            var totalRequests = 20;
            var cacheMisses = 0;
            var times = new List<long>();

            for (int i = 0; i < totalRequests; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await Client.GetAsync($"/api/people/{person.Id}");
                stopwatch.Stop();
                
                response.EnsureSuccessStatusCode();
                times.Add(stopwatch.ElapsedMilliseconds);
                
                // Consider it a cache miss if it takes more than 50ms
                if (stopwatch.ElapsedMilliseconds > 50)
                {
                    cacheMisses++;
                }
            }

            // Calculate statistics
            var averageTime = times.Average();
            var cacheHitRatio = (double)(totalRequests - cacheMisses) / totalRequests * 100;
            
            _output.WriteLine($"Total requests: {totalRequests}");
            _output.WriteLine($"Cache misses: {cacheMisses}");
            _output.WriteLine($"Cache hit ratio: {cacheHitRatio:F1}%");
            _output.WriteLine($"Average response time: {averageTime:F1}ms");
            _output.WriteLine($"Min time: {times.Min()}ms, Max time: {times.Max()}ms");

            // Assert
            cacheHitRatio.Should().BeGreaterThan(70, "Cache hit ratio should be above 70%");
            averageTime.Should().BeLessThan(50, "Average response time should be under 50ms");
        });
    }

    [Fact]
    public async Task DifferentEntityTypes_ShouldHaveIndependentCaches()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data for different entities
            var role = new Domain.Entities.Role { Name = "Test Role" };
            DbContext.Roles.Add(role);
            
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

            // Act - Cache both entity types
            var rolesResponse1 = await Client.GetAsync("/api/roles");
            var wallsResponse1 = await Client.GetAsync("/api/walls");
            rolesResponse1.EnsureSuccessStatusCode();
            wallsResponse1.EnsureSuccessStatusCode();

            // Modify roles (should only invalidate roles cache)
            var createRoleRequest = new { name = "Another Role", description = "Test" };
            await Client.PostAsJsonAsync("/api/roles", createRoleRequest);

            // Get both again
            var rolesResponse2 = await Client.GetAsync("/api/roles");
            var wallsResponse2 = await Client.GetAsync("/api/walls");

            var rolesContent1 = await rolesResponse1.Content.ReadAsStringAsync();
            var rolesContent2 = await rolesResponse2.Content.ReadAsStringAsync();
            var wallsContent1 = await wallsResponse1.Content.ReadAsStringAsync();
            var wallsContent2 = await wallsResponse2.Content.ReadAsStringAsync();

            // Assert
            rolesContent2.Should().NotBe(rolesContent1, "Roles cache should be invalidated");
            wallsContent2.Should().Be(wallsContent1, "Walls cache should NOT be invalidated");
        });
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldBenefitFromCaching()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            for (int i = 0; i < 5; i++)
            {
                var person = new Domain.Entities.Person 
                { 
                    FullName = $"Person {i}", 
                    Phone = $"555-050{i}" 
                };
                DbContext.People.Add(person);
            }
            await DbContext.SaveChangesAsync();

            // Warm up cache
            await Client.GetAsync("/api/people");

            // Act - Make concurrent requests
            var tasks = new Task<HttpResponseMessage>[10];
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Client.GetAsync("/api/people");
            }
            
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            foreach (var response in responses)
            {
                response.EnsureSuccessStatusCode();
            }

            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTime = totalTime / (double)tasks.Length;
            
            _output.WriteLine($"Concurrent requests: {tasks.Length}");
            _output.WriteLine($"Total time: {totalTime}ms");
            _output.WriteLine($"Average time per request: {averageTime:F1}ms");

            // With caching, concurrent requests should complete very quickly
            averageTime.Should().BeLessThan(20, 
                "Average time per concurrent cached request should be under 20ms");
        });
    }
}

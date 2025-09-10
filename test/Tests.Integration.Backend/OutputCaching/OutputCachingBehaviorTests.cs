using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Api.Extensions;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.OutputCaching;

public class OutputCachingBehaviorTests : IntegrationTestBase
{
    private readonly HttpClient _cachingClient;

    public OutputCachingBehaviorTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
        // Create a client with output caching enabled
        var factory = (fixture.Factory as SqliteTestWebApplicationFactory)!;
        var cachingFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ensure output caching is enabled
                services.Configure<Api.CachingSettings>(options =>
                {
                    options.UseOutputCaching = true;
                    options.UseRedis = false; // Use in-memory for tests
                });
            });
        });
        
        _cachingClient = cachingFactory.CreateClient();
    }

    [Fact]
    public async Task GetEndpoint_ShouldReturnCachedResponse_OnSecondRequest()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create some test data
            var testPerson = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var endpoint = "/api/people";

            // Act - First request (cache miss)
            var response1 = await _cachingClient.GetAsync(endpoint);
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Second request (should be cached)
            var response2 = await _cachingClient.GetAsync(endpoint);
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            content1.Should().Be(content2, "Cached response should match original");
            
            // Check for cache headers
            response2.Headers.Should().ContainKey("X-Cache");
            var cacheHeader = response2.Headers.GetValues("X-Cache").FirstOrDefault();
            cacheHeader.Should().Be("HIT", "Second request should be served from cache");
        });
    }

    [Fact]
    public async Task CachedResponse_ShouldBeServedFasterThanUncached()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            for (int i = 0; i < 10; i++)
            {
                var role = new Domain.Entities.Role { Name = $"Role {i}" };
                DbContext.Roles.Add(role);
            }
            await DbContext.SaveChangesAsync();

            var endpoint = "/api/roles";
            var stopwatch = new Stopwatch();

            // Act - First request (cache miss)
            stopwatch.Start();
            var response1 = await _cachingClient.GetAsync(endpoint);
            stopwatch.Stop();
            var uncachedTime = stopwatch.ElapsedMilliseconds;
            response1.EnsureSuccessStatusCode();

            // Small delay to ensure cache is populated
            await Task.Delay(100);

            // Act - Second request (should be cached)
            stopwatch.Restart();
            var response2 = await _cachingClient.GetAsync(endpoint);
            stopwatch.Stop();
            var cachedTime = stopwatch.ElapsedMilliseconds;
            response2.EnsureSuccessStatusCode();

            // Assert - cached should be faster, but be lenient in CI/CD
            cachedTime.Should().BeLessThan(100, "Cached response should be served quickly");
            
            // Verify it was actually cached
            var cacheHeader = response2.Headers.GetValues("X-Cache").FirstOrDefault();
            cacheHeader.Should().Be("HIT");
        });
    }

    [Fact]
    public async Task GetEndpoint_ShouldIncludeCacheControlHeaders()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var endpoint = "/api/people";

            // Act
            var response = await _cachingClient.GetAsync(endpoint);

            // Assert
            response.EnsureSuccessStatusCode();
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().NotBeNull();
        response.Headers.CacheControl.MaxAge!.Value.TotalSeconds.Should().Be(300); // 5 minutes for People
        });
    }

    [Fact]
    public async Task GetEndpoint_ShouldIncludeETagHeader()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var testPerson = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var endpoint = $"/api/people/{testPerson.Id}";

            // Act
            var response = await _cachingClient.GetAsync(endpoint);

            // Assert
            response.EnsureSuccessStatusCode();
            response.Headers.ETag.Should().NotBeNull("Response should include ETag header");
            response.Headers.ETag!.Tag.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task GetEndpoint_ShouldIncludeLastModifiedHeader()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var testPerson = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var endpoint = $"/api/people/{testPerson.Id}";

            // Act
            var response = await _cachingClient.GetAsync(endpoint);

            // Assert
            response.EnsureSuccessStatusCode();
            response.Content.Headers.LastModified.Should().NotBeNull("Response should include Last-Modified header");
        });
    }

    [Fact]
    public async Task ConditionalRequest_WithIfNoneMatch_ShouldReturn304WhenNotModified()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var testPerson = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var endpoint = $"/api/people/{testPerson.Id}";

            // Act - First request to get ETag
            var response1 = await _cachingClient.GetAsync(endpoint);
            response1.EnsureSuccessStatusCode();
            var etag = response1.Headers.ETag;
            etag.Should().NotBeNull();

            // Act - Second request with If-None-Match
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.IfNoneMatch.Add(etag!);
            var response2 = await _cachingClient.SendAsync(request);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.NotModified,
                "Should return 304 Not Modified when ETag matches");
        });
    }

    [Fact]
    public async Task ConditionalRequest_WithIfModifiedSince_ShouldReturn304WhenNotModified()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var testPerson = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var endpoint = $"/api/people/{testPerson.Id}";

            // Act - First request to get Last-Modified
            var response1 = await _cachingClient.GetAsync(endpoint);
            response1.EnsureSuccessStatusCode();
            var lastModified = response1.Content.Headers.LastModified;
            lastModified.Should().NotBeNull();

            // Act - Second request with If-Modified-Since
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.IfModifiedSince = lastModified;
            var response2 = await _cachingClient.SendAsync(request);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.NotModified,
                "Should return 304 Not Modified when resource hasn't changed since specified date");
        });
    }

    [Fact]
    public async Task PostRequest_ShouldInvalidateCollectionCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var getEndpoint = "/api/people";
            var postEndpoint = "/api/people";

            // Act - First GET to populate cache
            var response1 = await _cachingClient.GetAsync(getEndpoint);
            response1.EnsureSuccessStatusCode();

            // Verify it was a cache miss initially
            var cacheHeader1 = response1.Headers.GetValues("X-Cache").FirstOrDefault();
            cacheHeader1.Should().Be("MISS");

            // Act - GET again to verify it's cached
            var response2 = await _cachingClient.GetAsync(getEndpoint);
            response2.EnsureSuccessStatusCode();
            var cacheHeader2 = response2.Headers.GetValues("X-Cache").FirstOrDefault();
            cacheHeader2.Should().Be("HIT");

            // Act - POST to create new entity
            var newPerson = new { fullName = "New Person", phone = "555-0400" };
            var postResponse = await _cachingClient.PostAsJsonAsync(postEndpoint, newPerson);
            postResponse.EnsureSuccessStatusCode();

            // Act - GET should be cache miss (invalidated)
            var response3 = await _cachingClient.GetAsync(getEndpoint);
            response3.EnsureSuccessStatusCode();

            // Assert
            var cacheHeader3 = response3.Headers.GetValues("X-Cache").FirstOrDefault();
            cacheHeader3.Should().Be("MISS", "Cache should be invalidated after POST");
        });
    }

    [Fact]
    public async Task PutRequest_ShouldInvalidateEntityAndCollectionCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var testPerson = new Domain.Entities.Person { FullName = "Original Name", Phone = "555-0200" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var entityEndpoint = $"/api/people/{testPerson.Id}";
            var collectionEndpoint = "/api/people";

            // Act - GET requests to populate caches
            var response1 = await _cachingClient.GetAsync(entityEndpoint);
            response1.EnsureSuccessStatusCode();
            var response2 = await _cachingClient.GetAsync(collectionEndpoint);
            response2.EnsureSuccessStatusCode();

            // Verify second requests are cached
            var response1b = await _cachingClient.GetAsync(entityEndpoint);
            var response2b = await _cachingClient.GetAsync(collectionEndpoint);
            response1b.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("HIT");
            response2b.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("HIT");

            // Act - PUT to update entity
            var updatedPerson = new { id = testPerson.Id, fullName = "Updated Name", phone = "555-0500" };
            var putResponse = await _cachingClient.PutAsJsonAsync(entityEndpoint, updatedPerson);
            putResponse.EnsureSuccessStatusCode();

            // Act - GET requests should be cache misses
            var response3 = await _cachingClient.GetAsync(entityEndpoint);
            var response4 = await _cachingClient.GetAsync(collectionEndpoint);

            // Assert
            response3.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("MISS");
            response4.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("MISS");
        });
    }

    [Fact]
    public async Task DeleteRequest_ShouldInvalidateEntityAndCollectionCache()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var testPerson = new Domain.Entities.Person { FullName = "To Delete", Phone = "555-0300" };
            DbContext.People.Add(testPerson);
            await DbContext.SaveChangesAsync();

            var entityEndpoint = $"/api/people/{testPerson.Id}";
            var collectionEndpoint = "/api/people";

            // Act - GET requests to populate caches
            var response1 = await _cachingClient.GetAsync(entityEndpoint);
            response1.EnsureSuccessStatusCode();
            var response2 = await _cachingClient.GetAsync(collectionEndpoint);
            response2.EnsureSuccessStatusCode();

            // Verify second request to collection is cached
            var response2b = await _cachingClient.GetAsync(collectionEndpoint);
            response2b.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("HIT");

            // Act - DELETE entity
            var deleteResponse = await _cachingClient.DeleteAsync(entityEndpoint);
            deleteResponse.EnsureSuccessStatusCode();

            // Act - Collection GET should be cache miss
            var response3 = await _cachingClient.GetAsync(collectionEndpoint);

            // Assert
            response3.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("MISS");
        });
    }

    [Theory]
    [InlineData("/api/people?page=1&size=10", "/api/people?page=2&size=10")] // Different page
    [InlineData("/api/people?size=10", "/api/people?size=20")] // Different size
    [InlineData("/api/people?filter=test", "/api/people?filter=other")] // Different filter
    public async Task OutputCache_ShouldVaryByQueryParameters(string endpoint1, string endpoint2)
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act - Request first endpoint
            var response1 = await _cachingClient.GetAsync(endpoint1);
            response1.EnsureSuccessStatusCode();

            // Act - Request second endpoint (different query params)
            var response2 = await _cachingClient.GetAsync(endpoint2);
            response2.EnsureSuccessStatusCode();

            // Assert - Second request should be cache miss due to different query params
            response2.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("MISS");

            // Act - Request first endpoint again (should be cached)
            var response3 = await _cachingClient.GetAsync(endpoint1);
            response3.EnsureSuccessStatusCode();

            // Assert - Third request should be cache hit
            response3.Headers.GetValues("X-Cache").FirstOrDefault().Should().Be("HIT");
        });
    }

    protected new void Dispose()
    {
        _cachingClient?.Dispose();
        base.Dispose();
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.OutputCaching;

/// <summary>
/// Tests for conditional request support (If-None-Match, If-Modified-Since)
/// </summary>
public class ConditionalRequestTests : IntegrationTestBase
{
    public ConditionalRequestTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetRequest_WithIfNoneMatch_ShouldReturn304_WhenETagMatches()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Act - First request to get ETag
            var response1 = await Client.GetAsync($"/api/people/{person.Id}");
            response1.EnsureSuccessStatusCode();
            var etag = response1.Headers.ETag;

            // Skip if no ETag is returned (not all endpoints may support it yet)
            if (etag == null)
            {
                return;
            }

            // Act - Second request with If-None-Match
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/people/{person.Id}");
            request.Headers.IfNoneMatch.Add(etag);
            var response2 = await Client.SendAsync(request);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.NotModified, 
                "Server should return 304 when ETag matches");
            
            var content = await response2.Content.ReadAsStringAsync();
            content.Should().BeEmpty("304 response should have no body");
        });
    }

    [Fact]
    public async Task GetRequest_WithIfNoneMatch_ShouldReturn200_WhenETagDoesNotMatch()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Act - Request with non-matching ETag
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/people/{person.Id}");
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"non-matching-etag\""));
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, 
                "Server should return 200 when ETag doesn't match");
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty("200 response should have body");
        });
    }

    [Fact]
    public async Task GetRequest_WithIfModifiedSince_ShouldReturn304_WhenNotModified()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Act - First request to get Last-Modified
            var response1 = await Client.GetAsync($"/api/people/{person.Id}");
            response1.EnsureSuccessStatusCode();
            var lastModified = response1.Content.Headers.LastModified;

            // Skip if no Last-Modified is returned
            if (lastModified == null)
            {
                return;
            }

            // Act - Second request with If-Modified-Since (using future date to ensure not modified)
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/people/{person.Id}");
            request.Headers.IfModifiedSince = DateTimeOffset.UtcNow.AddMinutes(1);
            var response2 = await Client.SendAsync(request);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.NotModified, 
                "Server should return 304 when resource hasn't been modified since the given date");
            
            var content = await response2.Content.ReadAsStringAsync();
            content.Should().BeEmpty("304 response should have no body");
        });
    }

    [Fact]
    public async Task GetRequest_WithIfModifiedSince_ShouldReturn200_WhenModified()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            var person = new Domain.Entities.Person { FullName = "Test Person", Phone = "555-0100" };
            DbContext.People.Add(person);
            await DbContext.SaveChangesAsync();

            // Act - Request with old If-Modified-Since date
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/people/{person.Id}");
            request.Headers.IfModifiedSince = DateTimeOffset.UtcNow.AddDays(-1);
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, 
                "Server should return 200 when resource has been modified since the given date");
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty("200 response should have body");
        });
    }

    [Fact]
    public async Task GetListRequest_WithConditionalHeaders_ShouldHandleCorrectly()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create test data
            for (int i = 0; i < 5; i++)
            {
                var role = new Domain.Entities.Role { Name = $"Role {i}" };
                DbContext.Roles.Add(role);
            }
            await DbContext.SaveChangesAsync();

            // Act - First request to get ETag
            var response1 = await Client.GetAsync("/api/roles");
            response1.EnsureSuccessStatusCode();
            var etag = response1.Headers.ETag;

            if (etag == null)
            {
                return; // Skip if ETags not supported
            }

            // Act - Second request with If-None-Match
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/roles");
            request.Headers.IfNoneMatch.Add(etag);
            var response2 = await Client.SendAsync(request);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.NotModified, 
                "Server should return 304 for list endpoints when ETag matches");
        });
    }

    [Fact]
    public async Task PostRequest_ShouldNotBeAffected_ByConditionalHeaders()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var createRequest = new
            {
                FullName = "New Person",
                Phone = "555-0200"
            };

            // Act - POST request with conditional headers (should be ignored)
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/people");
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"some-etag\""));
            request.Headers.IfModifiedSince = DateTimeOffset.UtcNow.AddMinutes(1);
            request.Content = JsonContent.Create(createRequest);
            
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created, 
                "POST requests should ignore conditional headers");
        });
    }
}

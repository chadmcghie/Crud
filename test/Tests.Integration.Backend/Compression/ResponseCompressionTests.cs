using System.Net.Http.Headers;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Compression;

/// <summary>
/// Basic integration test to verify response compression is configured and working
/// for application API endpoints. Focuses on application functionality rather than
/// detailed compression framework behavior.
/// </summary>
public class ResponseCompressionTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public ResponseCompressionTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApiResponse_ShouldBeCompressed_WhenClientSupportsCompression()
    {
        // Arrange
        var authenticatedClient = await AuthenticationTestHelper.CreateUserClientAsync(_factory);
        authenticatedClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await authenticatedClient.GetAsync("/api/people");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("gzip", response.Content.Headers.ContentEncoding);
    }
}

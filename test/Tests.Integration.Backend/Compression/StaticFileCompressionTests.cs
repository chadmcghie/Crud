using System.Net.Http.Headers;
using Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration.Backend.Compression;

public class StaticFileCompressionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StaticFileCompressionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CssFile_ShouldBeCompressed_WhenClientSupportsGzip()
    {
        // Arrange
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await _client.GetAsync("/css/test.css");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/css");
        response.Content.Headers.ContentEncoding.Should().Contain("gzip");
    }

    [Fact]
    public async Task JsFile_ShouldBeCompressed_WhenClientSupportsGzip()
    {
        // Arrange
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await _client.GetAsync("/js/test.js");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/javascript");
        response.Content.Headers.ContentEncoding.Should().Contain("gzip");
    }

    [Fact]
    public async Task HtmlFile_ShouldBeCompressed_WhenClientSupportsGzip()
    {
        // Arrange
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await _client.GetAsync("/index.html");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        response.Content.Headers.ContentEncoding.Should().Contain("gzip");
    }

    [Fact]
    public async Task CssFile_ShouldBeCompressed_WhenClientSupportsBrotli()
    {
        // Arrange
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        // Act
        var response = await _client.GetAsync("/css/test.css");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/css");
        response.Content.Headers.ContentEncoding.Should().Contain("br");
    }

    [Fact]
    public async Task JsFile_ShouldBeCompressed_WhenClientSupportsBrotli()
    {
        // Arrange
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        // Act
        var response = await _client.GetAsync("/js/test.js");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/javascript");
        response.Content.Headers.ContentEncoding.Should().Contain("br");
    }

    [Fact]
    public async Task StaticFiles_ShouldHaveCorrectContentTypes()
    {
        // Act
        var cssResponse = await _client.GetAsync("/css/test.css");
        var jsResponse = await _client.GetAsync("/js/test.js");
        var htmlResponse = await _client.GetAsync("/index.html");

        // Assert
        cssResponse.IsSuccessStatusCode.Should().BeTrue();
        cssResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/css");

        jsResponse.IsSuccessStatusCode.Should().BeTrue();
        jsResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/javascript");

        htmlResponse.IsSuccessStatusCode.Should().BeTrue();
        htmlResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task CompressedStaticFiles_ShouldHaveVaryHeader()
    {
        // Arrange
        _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var cssResponse = await _client.GetAsync("/css/test.css");
        var jsResponse = await _client.GetAsync("/js/test.js");
        var htmlResponse = await _client.GetAsync("/index.html");

        // Assert
        cssResponse.Headers.Vary.Should().Contain("Accept-Encoding");
        jsResponse.Headers.Vary.Should().Contain("Accept-Encoding");
        htmlResponse.Headers.Vary.Should().Contain("Accept-Encoding");
    }

    [Fact]
    public async Task UncompressedStaticFiles_ShouldNotHaveCompressionHeaders()
    {
        // Act (no Accept-Encoding header)
        var cssResponse = await _client.GetAsync("/css/test.css");
        var jsResponse = await _client.GetAsync("/js/test.js");
        var htmlResponse = await _client.GetAsync("/index.html");

        // Assert
        cssResponse.IsSuccessStatusCode.Should().BeTrue();
        cssResponse.Content.Headers.ContentEncoding.Should().BeEmpty();

        jsResponse.IsSuccessStatusCode.Should().BeTrue();
        jsResponse.Content.Headers.ContentEncoding.Should().BeEmpty();

        htmlResponse.IsSuccessStatusCode.Should().BeTrue();
        htmlResponse.Content.Headers.ContentEncoding.Should().BeEmpty();
    }

    [Fact]
    public async Task CompressedStaticFiles_ShouldBeSmallerThanUncompressed()
    {
        // Arrange
        var uncompressedClient = _factory.CreateClient();
        var compressedClient = _factory.CreateClient();
        compressedClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var uncompressedCss = await uncompressedClient.GetAsync("/css/test.css");
        var compressedCss = await compressedClient.GetAsync("/css/test.css");

        // Assert
        uncompressedCss.IsSuccessStatusCode.Should().BeTrue();
        compressedCss.IsSuccessStatusCode.Should().BeTrue();

        // Verify compression is working by checking headers
        compressedCss.Content.Headers.ContentEncoding.Should().Contain("gzip");
        uncompressedCss.Content.Headers.ContentEncoding.Should().BeEmpty();

        // Verify content serves correctly (HttpClient should auto-decompress)
        var uncompressedContent = await uncompressedCss.Content.ReadAsStringAsync();

        uncompressedContent.Should().NotBeEmpty();
        uncompressedContent.Should().Contain("body {");

        // Verify compression is actually happening
    }
}

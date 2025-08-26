using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.Integration.Backend;

public class FluentValidationIntegrationTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;
    private readonly HttpClient _client;

    public FluentValidationIntegrationTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_People_WithInvalidData_ShouldReturnValidationErrors()
    {
        // Arrange
        var invalidPerson = new
        {
            fullName = "",
            phone = "invalid-phone",
            roleIds = new List<Guid>()
        };

        var json = JsonSerializer.Serialize(invalidPerson);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/People", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Full name is required", responseBody);
        Assert.Contains("Phone number must be a valid format", responseBody);
    }

    [Fact]
    public async Task POST_People_WithValidData_ShouldSucceed()
    {
        // Arrange
        var validPerson = new
        {
            fullName = "John Doe",
            phone = "1234567890",
            roleIds = new List<Guid>()
        };

        var json = JsonSerializer.Serialize(validPerson);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/People", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("John Doe", responseBody);
        Assert.Contains("1234567890", responseBody);
    }

    [Fact]
    public async Task POST_Roles_WithInvalidData_ShouldReturnValidationErrors()
    {
        // Arrange
        var invalidRole = new
        {
            name = "",
            description = new string('A', 501) // Too long
        };

        var json = JsonSerializer.Serialize(invalidRole);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Roles", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Role name is required", responseBody);
        Assert.Contains("Description cannot exceed 500 characters", responseBody);
    }

    [Fact]
    public async Task POST_Roles_WithValidData_ShouldSucceed()
    {
        // Arrange
        var validRole = new
        {
            name = "Administrator",
            description = "System administrator role"
        };

        var json = JsonSerializer.Serialize(validRole);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Roles", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Administrator", responseBody);
        Assert.Contains("System administrator role", responseBody);
    }
}
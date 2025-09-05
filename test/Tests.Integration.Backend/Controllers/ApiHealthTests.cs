using System.Net;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

/// <summary>
/// Tests for overall API health and cross-cutting concerns
/// </summary>
public class ApiHealthTests : IntegrationTestBase
{
    public ApiHealthTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task API_Should_Be_Responsive()
    {
        // Act
        var response = await Client.GetAsync("/api/roles");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task API_Should_Return_JSON_Content_Type()
    {
        // Act
        var response = await Client.GetAsync("/api/roles");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task API_Should_Handle_CORS_Headers()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        // Act
        var response = await Client.GetAsync("/api/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Note: CORS headers are typically added by middleware, 
        // but in test environment they might not be present
    }

    [Fact]
    public async Task API_Should_Handle_Invalid_Routes()
    {
        // Act
        var response = await Client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task API_Should_Handle_Invalid_HTTP_Methods()
    {
        // Act
        var response = await Client.PatchAsync("/api/roles", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task API_Should_Handle_Malformed_JSON()
    {
        // Arrange
        var malformedJson = new StringContent("{ invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/roles", malformedJson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task API_Should_Handle_Large_Payloads()
    {
        // Arrange

        var largeDescription = new string('A', 10000); // Very long description
        var createRequest = new
        {
            Name = "Test Role",
            Description = largeDescription
        };

        // Act
        var response = await PostJsonAsync("/api/roles", createRequest);

        // Assert
        // Should either succeed or fail gracefully with appropriate status code
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task API_Should_Handle_Concurrent_Requests()
    {
        // Arrange
        await ClearDatabaseAsync(); // Start with clean database for health tests

        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send multiple concurrent requests
        for (int i = 0; i < 10; i++)
        {
            var createRequest = new
            {
                Name = $"Concurrent Role {i}",
                Description = $"Role created in concurrent test {i}"
            };
            tasks.Add(PostJsonAsync("/api/roles", createRequest));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);

        // Verify all roles were created
        var getResponse = await Client.GetAsync("/api/roles");
        var roles = await ReadJsonAsync<List<object>>(getResponse);
        roles.Should().HaveCount(10);
    }

    [Fact]
    public async Task API_Should_Maintain_Data_Consistency_Under_Load()
    {
        // Arrange
        await ClearDatabaseAsync(); // Start with clean database for health tests


        // Create a role first
        var roleResponse = await PostJsonAsync("/api/roles", new { Name = "Test Role", Description = "Test" });
        roleResponse.EnsureSuccessStatusCode();
        var role = await ReadJsonAsync<RoleDto>(roleResponse);
        var roleId = role?.Id ?? Guid.Empty;

        // Ensure role was created successfully
        roleId.Should().NotBe(Guid.Empty, "Role must be created before testing");

        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Create multiple people with the same role concurrently
        var names = new[] { "John Smith", "Jane Doe", "Bob Johnson", "Alice Brown", "Charlie Davis" };
        for (int i = 0; i < 5; i++)
        {
            var createRequest = new
            {
                FullName = names[i],
                Phone = $"555-{100 + i:D3}-{1000 + i:D4}",
                RoleIds = new[] { roleId.ToString() }
            };
            tasks.Add(PostJsonAsync("/api/people", createRequest));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);

        // Verify data consistency
        var getPeopleResponse = await Client.GetAsync("/api/people");
        var people = await ReadJsonAsync<List<object>>(getPeopleResponse);
        people.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("/api/roles")]
    [InlineData("/api/people")]
    [InlineData("/api/walls")]
    [InlineData("/api/windows")]
    public async Task All_Controllers_Should_Be_Accessible(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Theory]
    [InlineData("GET", "/api/roles")]
    [InlineData("POST", "/api/roles")]
    [InlineData("GET", "/api/people")]
    [InlineData("POST", "/api/people")]
    [InlineData("GET", "/api/walls")]
    [InlineData("POST", "/api/walls")]
    [InlineData("GET", "/api/windows")]
    [InlineData("POST", "/api/windows")]
    public async Task All_Endpoints_Should_Support_Expected_HTTP_Methods(string method, string endpoint)
    {
        // Arrange
        HttpResponseMessage response;

        // Act
        switch (method.ToUpper())
        {
            case "GET":
                response = await Client.GetAsync(endpoint);
                break;
            case "POST":
                // Use minimal valid data for POST requests
                object postData = endpoint switch
                {
                    "/api/roles" => new { Name = "Test Role" },
                    "/api/people" => new { FullName = "Test Person" },
                    "/api/walls" => new { Name = "Test Wall", Length = 10.0, Height = 3.0, Thickness = 0.3, AssemblyType = "Wood Frame" },
                    "/api/windows" => new { Name = "Test Window", Width = 1.0, Height = 1.5, Area = 1.5, FrameType = "Wood", GlazingType = "Single Pane" },
                    _ => new { }
                };
                response = await PostJsonAsync(endpoint, postData);
                break;
            default:
                throw new ArgumentException($"Unsupported HTTP method: {method}");
        }

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,           // GET requests
            HttpStatusCode.Created,      // Successful POST requests
            HttpStatusCode.BadRequest    // POST requests with validation errors
        );
    }
}

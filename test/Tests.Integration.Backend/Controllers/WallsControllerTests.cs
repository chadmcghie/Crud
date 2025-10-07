using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

public class WallsControllerTests : IntegrationTestBase
{
    public WallsControllerTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_Walls_Should_Return_Empty_List_Initially()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange


            // Act
            var response = await AuthenticatedGetAsync("/api/walls");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var walls = await ReadJsonAsync<List<WallResponse>>(response);
            Assert.NotNull(walls);
            Assert.Empty(walls);
        });
    }

    [Fact]
    public async Task POST_Walls_Should_Create_Wall_And_Return_201()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest(
            name: "Exterior Wall",
            description: "Main exterior wall",
            length: 10.5,
            height: 3.0,
            thickness: 0.3,
            assemblyType: "Wood Frame",
            assemblyDetails: "2x6 wood studs",
            rValue: 20.0,
            uValue: 0.05,
            materialLayers: "Siding, Sheathing, Insulation, Drywall",
            orientation: "North",
            location: "Front"
        );

        // Act
        var response = await AuthenticatedPostJsonAsync("/api/walls", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdWall = await ReadJsonAsync<WallResponse>(response);

        Assert.NotNull(createdWall);
        Assert.NotEqual(Guid.Empty, createdWall!.Id);
        Assert.Equal("Exterior Wall", createdWall.Name);
        Assert.Equal("Main exterior wall", createdWall.Description);
        Assert.Equal(10.5, createdWall.Length);
        Assert.Equal(3.0, createdWall.Height);
        Assert.Equal(0.3, createdWall.Thickness);
        Assert.Equal("Wood Frame", createdWall.AssemblyType);
        Assert.Equal(20.0, createdWall.RValue);
        Assert.Equal(0.05, createdWall.UValue);
        Assert.True((DateTime.UtcNow - createdWall.CreatedAt).TotalMinutes < 1);

        // Verify location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/walls/{createdWall.Id}".ToLowerInvariant(), response.Headers.Location!.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task POST_Walls_Should_Return_400_For_Invalid_Data()
    {
        // Arrange

        var invalidRequest = new
        {
            Name = "", // Empty name - should fail validation
            Length = 10.0,
            Height = 3.0,
            Thickness = 0.3,
            AssemblyType = "Wood Frame"
        };

        // Act
        var response = await AuthenticatedPostJsonAsync("/api/walls", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Walls_Should_Validate_Numeric_Ranges()
    {
        // Arrange

        var invalidRequest = TestDataBuilders.CreateWallRequest(
            name: "Test Wall",
            length: -1.0, // Invalid - should be > 0.1
            height: 3.0,
            thickness: 0.3,
            assemblyType: "Wood Frame"
        );

        // Act
        var response = await AuthenticatedPostJsonAsync("/api/walls", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Walls_Should_Return_All_Walls()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange


            // Create test walls
            var wall1 = TestDataBuilders.CreateWallRequest("Wall 1", "First wall", 10.0, 3.0, 0.3, "Wood Frame");
            var wall2 = TestDataBuilders.CreateWallRequest("Wall 2", "Second wall", 8.0, 2.5, 0.25, "Steel Frame");

            await AuthenticatedPostJsonAsync("/api/walls", wall1);
            await AuthenticatedPostJsonAsync("/api/walls", wall2);

            // Act
            var response = await AuthenticatedGetAsync("/api/walls");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var walls = await ReadJsonAsync<List<WallResponse>>(response);

            Assert.NotNull(walls);
            Assert.Equal(2, walls.Count);
            Assert.Contains(walls, w => w.Name == "Wall 1");
            Assert.Contains(walls, w => w.Name == "Wall 2");
        });
    }

    [Fact]
    public async Task GET_Wall_By_Id_Should_Return_Wall_When_Exists()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest("Test Wall", "Test description", 12.0, 3.5, 0.4, "Concrete");
        var createResponse = await AuthenticatedPostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);

        // Act
        var response = await AuthenticatedGetAsync($"/api/walls/{createdWall!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wall = await ReadJsonAsync<WallResponse>(response);

        Assert.NotNull(wall);
        Assert.Equal(createdWall.Id, wall!.Id);
        Assert.Equal("Test Wall", wall.Name);
        Assert.Equal("Test description", wall.Description);
        Assert.Equal(12.0, wall.Length);
        Assert.Equal(3.5, wall.Height);
        Assert.Equal(0.4, wall.Thickness, 4);
        Assert.Equal("Concrete", wall.AssemblyType);
    }

    [Fact]
    public async Task GET_Wall_By_Id_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await AuthenticatedGetAsync($"/api/walls/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Wall_Should_Update_Existing_Wall()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest("Original Wall", "Original description", 10.0, 3.0, 0.3, "Wood Frame");
        var createResponse = await AuthenticatedPostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);

        var updateRequest = TestDataBuilders.UpdateWallRequest(
            name: "Updated Wall",
            description: "Updated description",
            length: 15.0,
            height: 4.0,
            thickness: 0.4,
            assemblyType: "Steel Frame",
            rValue: 25.0,
            uValue: 0.04
        );

        // Act
        var response = await AuthenticatedPutJsonAsync($"/api/walls/{createdWall!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await AuthenticatedGetAsync($"/api/walls/{createdWall.Id}");
        var updatedWall = await ReadJsonAsync<WallResponse>(getResponse);

        Assert.NotNull(updatedWall);
        Assert.Equal("Updated Wall", updatedWall!.Name);
        Assert.Equal("Updated description", updatedWall.Description);
        Assert.Equal(15.0, updatedWall.Length);
        Assert.Equal(4.0, updatedWall.Height);
        Assert.Equal(0.4, updatedWall.Thickness, 4);
        Assert.Equal("Steel Frame", updatedWall.AssemblyType);
        Assert.Equal(25.0, updatedWall.RValue);
        Assert.Equal(0.04, updatedWall.UValue!.Value, 4);
        Assert.NotNull(updatedWall.UpdatedAt);
        Assert.True((DateTime.UtcNow - updatedWall.UpdatedAt.Value).TotalMinutes < 1);
    }

    [Fact]
    public async Task PUT_Wall_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdateWallRequest("Updated Wall", "Updated description", 15.0, 4.0, 0.4, "Steel Frame");

        // Act
        var response = await AuthenticatedPutJsonAsync($"/api/walls/{nonExistentId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Wall_Should_Remove_Existing_Wall()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest("To Delete", "Wall to be deleted", 10.0, 3.0, 0.3, "Wood Frame");
        var createResponse = await AuthenticatedPostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);

        // Act
        var response = await AuthenticatedDeleteAsync($"/api/walls/{createdWall!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the wall is deleted
        var getResponse = await AuthenticatedGetAsync($"/api/walls/{createdWall.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DELETE_Wall_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await AuthenticatedDeleteAsync($"/api/walls/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task POST_Wall_Should_Handle_Optional_Fields()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest(
            name: "Simple Wall",
            description: null,
            length: 10.0,
            height: 3.0,
            thickness: 0.3,
            assemblyType: "Wood Frame",
            assemblyDetails: null,
            rValue: null,
            uValue: null,
            materialLayers: null,
            orientation: null,
            location: null
        );

        // Act
        var response = await AuthenticatedPostJsonAsync("/api/walls", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdWall = await ReadJsonAsync<WallResponse>(response);

        Assert.NotNull(createdWall);
        Assert.Equal("Simple Wall", createdWall!.Name);
        Assert.Null(createdWall.Description);
        Assert.Null(createdWall.AssemblyDetails);
        Assert.Null(createdWall.RValue);
        Assert.Null(createdWall.UValue);
        Assert.Null(createdWall.MaterialLayers);
        Assert.Null(createdWall.Orientation);
        Assert.Null(createdWall.Location);
    }

    [Fact]
    public async Task Walls_Should_Persist_Timestamps_Correctly()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest("Timestamp Test", "Test timestamps", 10.0, 3.0, 0.3, "Wood Frame");

        // Act - Create wall
        var createResponse = await AuthenticatedPostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);
        var createdAt = createdWall!.CreatedAt;

        // Wait a moment to ensure different timestamps
        await Task.Delay(100);

        // Act - Update wall
        var updateRequest = TestDataBuilders.UpdateWallRequest("Updated Timestamp Test", "Updated timestamps", 12.0, 3.5, 0.35, "Steel Frame");
        await AuthenticatedPutJsonAsync($"/api/walls/{createdWall.Id}", updateRequest);

        // Act - Get updated wall
        var getResponse = await AuthenticatedGetAsync($"/api/walls/{createdWall.Id}");
        var updatedWall = await ReadJsonAsync<WallResponse>(getResponse);

        // Assert
        Assert.NotNull(updatedWall);
        Assert.Equal(createdAt, updatedWall!.CreatedAt); // CreatedAt should not change
        Assert.NotNull(updatedWall.UpdatedAt);
        Assert.True(updatedWall.UpdatedAt > createdAt); // UpdatedAt should be after CreatedAt
    }

    [Fact]
    public async Task POST_Wall_Should_Calculate_And_Store_Complex_Properties()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWallRequest(
            name: "Complex Wall",
            description: "Wall with complex properties",
            length: 12.5,
            height: 3.2,
            thickness: 0.35,
            assemblyType: "Insulated Concrete Form",
            assemblyDetails: "ICF with steel reinforcement",
            rValue: 22.5,
            uValue: 0.044,
            materialLayers: "Concrete, Foam Insulation, Concrete",
            orientation: "South-East",
            location: "Living Room"
        );

        // Act
        var response = await AuthenticatedPostJsonAsync("/api/walls", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdWall = await ReadJsonAsync<WallResponse>(response);

        Assert.NotNull(createdWall);
        Assert.Equal(12.5, createdWall!.Length);
        Assert.Equal(3.2, createdWall.Height);
        Assert.Equal(0.35, createdWall.Thickness);
        Assert.Equal(22.5, createdWall.RValue);
        Assert.Equal(0.044, createdWall.UValue);
        Assert.Equal("Concrete, Foam Insulation, Concrete", createdWall.MaterialLayers);
        Assert.Equal("South-East", createdWall.Orientation);
        Assert.Equal("Living Room", createdWall.Location);
    }
}

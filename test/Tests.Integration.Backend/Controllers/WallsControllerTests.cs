using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

public class WallsControllerTests : IntegrationTestBase
{
    public WallsControllerTests(SharedSqlServerWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_Walls_Should_Return_Empty_List_Initially()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            

            // Act
            var response = await Client.GetAsync("/api/walls");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var walls = await ReadJsonAsync<List<WallResponse>>(response);
            walls.Should().NotBeNull();
            walls.Should().BeEmpty();
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
        var response = await PostJsonAsync("/api/walls", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWall = await ReadJsonAsync<WallResponse>(response);
        
        createdWall.Should().NotBeNull();
        createdWall!.Id.Should().NotBeEmpty();
        createdWall.Name.Should().Be("Exterior Wall");
        createdWall.Description.Should().Be("Main exterior wall");
        createdWall.Length.Should().Be(10.5);
        createdWall.Height.Should().Be(3.0);
        createdWall.Thickness.Should().Be(0.3);
        createdWall.AssemblyType.Should().Be("Wood Frame");
        createdWall.RValue.Should().Be(20.0);
        createdWall.UValue.Should().Be(0.05);
        createdWall.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain($"/api/walls/{createdWall.Id}".ToLowerInvariant());
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
        var response = await PostJsonAsync("/api/walls", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        var response = await PostJsonAsync("/api/walls", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
            
            await PostJsonAsync("/api/walls", wall1);
            await PostJsonAsync("/api/walls", wall2);

            // Act
            var response = await Client.GetAsync("/api/walls");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var walls = await ReadJsonAsync<List<WallResponse>>(response);
            
            walls.Should().NotBeNull();
            walls.Should().HaveCount(2);
            walls.Should().Contain(w => w.Name == "Wall 1");
            walls.Should().Contain(w => w.Name == "Wall 2");
        });
    }

    [Fact]
    public async Task GET_Wall_By_Id_Should_Return_Wall_When_Exists()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWallRequest("Test Wall", "Test description", 12.0, 3.5, 0.4, "Concrete");
        var createResponse = await PostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/walls/{createdWall!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wall = await ReadJsonAsync<WallResponse>(response);
        
        wall.Should().NotBeNull();
        wall!.Id.Should().Be(createdWall.Id);
        wall.Name.Should().Be("Test Wall");
        wall.Description.Should().Be("Test description");
        wall.Length.Should().Be(12.0);
        wall.Height.Should().Be(3.5);
        wall.Thickness.Should().BeApproximately(0.4, 0.0001);
        wall.AssemblyType.Should().Be("Concrete");
    }

    [Fact]
    public async Task GET_Wall_By_Id_Should_Return_404_When_Not_Exists()
    {
        // Arrange
        
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/walls/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Wall_Should_Update_Existing_Wall()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWallRequest("Original Wall", "Original description", 10.0, 3.0, 0.3, "Wood Frame");
        var createResponse = await PostJsonAsync("/api/walls", createRequest);
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
        var response = await PutJsonAsync($"/api/walls/{createdWall!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/walls/{createdWall.Id}");
        var updatedWall = await ReadJsonAsync<WallResponse>(getResponse);
        
        updatedWall.Should().NotBeNull();
        updatedWall!.Name.Should().Be("Updated Wall");
        updatedWall.Description.Should().Be("Updated description");
        updatedWall.Length.Should().Be(15.0);
        updatedWall.Height.Should().Be(4.0);
        updatedWall.Thickness.Should().BeApproximately(0.4, 0.0001);
        updatedWall.AssemblyType.Should().Be("Steel Frame");
        updatedWall.RValue.Should().Be(25.0);
        updatedWall.UValue.Should().BeApproximately(0.04, 0.0001);
        updatedWall.UpdatedAt.Should().NotBeNull();
        updatedWall.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PUT_Wall_Should_Return_404_When_Not_Exists()
    {
        // Arrange
        
        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdateWallRequest("Updated Wall", "Updated description", 15.0, 4.0, 0.4, "Steel Frame");

        // Act
        var response = await PutJsonAsync($"/api/walls/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Wall_Should_Remove_Existing_Wall()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWallRequest("To Delete", "Wall to be deleted", 10.0, 3.0, 0.3, "Wood Frame");
        var createResponse = await PostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/walls/{createdWall!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the wall is deleted
        var getResponse = await Client.GetAsync($"/api/walls/{createdWall.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Wall_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/walls/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
        var response = await PostJsonAsync("/api/walls", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWall = await ReadJsonAsync<WallResponse>(response);
        
        createdWall.Should().NotBeNull();
        createdWall!.Name.Should().Be("Simple Wall");
        createdWall.Description.Should().BeNull();
        createdWall.AssemblyDetails.Should().BeNull();
        createdWall.RValue.Should().BeNull();
        createdWall.UValue.Should().BeNull();
        createdWall.MaterialLayers.Should().BeNull();
        createdWall.Orientation.Should().BeNull();
        createdWall.Location.Should().BeNull();
    }

    [Fact]
    public async Task Walls_Should_Persist_Timestamps_Correctly()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWallRequest("Timestamp Test", "Test timestamps", 10.0, 3.0, 0.3, "Wood Frame");

        // Act - Create wall
        var createResponse = await PostJsonAsync("/api/walls", createRequest);
        var createdWall = await ReadJsonAsync<WallResponse>(createResponse);
        var createdAt = createdWall!.CreatedAt;

        // Wait a moment to ensure different timestamps
        await Task.Delay(100);

        // Act - Update wall
        var updateRequest = TestDataBuilders.UpdateWallRequest("Updated Timestamp Test", "Updated timestamps", 12.0, 3.5, 0.35, "Steel Frame");
        await PutJsonAsync($"/api/walls/{createdWall.Id}", updateRequest);

        // Act - Get updated wall
        var getResponse = await Client.GetAsync($"/api/walls/{createdWall.Id}");
        var updatedWall = await ReadJsonAsync<WallResponse>(getResponse);

        // Assert
        updatedWall.Should().NotBeNull();
        updatedWall!.CreatedAt.Should().Be(createdAt); // CreatedAt should not change
        updatedWall.UpdatedAt.Should().NotBeNull();
        updatedWall.UpdatedAt.Should().BeAfter(createdAt); // UpdatedAt should be after CreatedAt
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
        var response = await PostJsonAsync("/api/walls", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWall = await ReadJsonAsync<WallResponse>(response);
        
        createdWall.Should().NotBeNull();
        createdWall!.Length.Should().Be(12.5);
        createdWall.Height.Should().Be(3.2);
        createdWall.Thickness.Should().Be(0.35);
        createdWall.RValue.Should().Be(22.5);
        createdWall.UValue.Should().Be(0.044);
        createdWall.MaterialLayers.Should().Be("Concrete, Foam Insulation, Concrete");
        createdWall.Orientation.Should().Be("South-East");
        createdWall.Location.Should().Be("Living Room");
    }
}

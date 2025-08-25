using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

public class WindowsControllerTests : IntegrationTestBase
{
    public WindowsControllerTests(SharedSqlServerWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_Windows_Should_Return_Empty_List_Initially()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            

            // Act
            var response = await Client.GetAsync("/api/windows");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var windows = await ReadJsonAsync<List<WindowResponse>>(response);
            windows.Should().NotBeNull();
            windows.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task POST_Windows_Should_Create_Window_And_Return_201()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest(
            name: "Living Room Window",
            description: "Large south-facing window",
            width: 1.5,
            height: 2.0,
            area: 3.0,
            frameType: "Vinyl",
            frameDetails: "Double-hung vinyl frame",
            glazingType: "Double Pane",
            glazingDetails: "Low-E coating with argon fill",
            uValue: 0.3,
            solarHeatGainCoefficient: 0.25,
            visibleTransmittance: 0.7,
            airLeakage: 0.1,
            energyStarRating: "Most Efficient",
            nfrcRating: "A+",
            orientation: "South",
            location: "Living Room",
            installationType: "New Construction",
            operationType: "Double Hung",
            hasScreens: true,
            hasStormWindows: false
        );

        // Act
        var response = await PostJsonAsync("/api/windows", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWindow = await ReadJsonAsync<WindowResponse>(response);
        
        createdWindow.Should().NotBeNull();
        createdWindow!.Id.Should().NotBeEmpty();
        createdWindow.Name.Should().Be("Living Room Window");
        createdWindow.Description.Should().Be("Large south-facing window");
        createdWindow.Width.Should().Be(1.5);
        createdWindow.Height.Should().Be(2.0);
        createdWindow.Area.Should().Be(3.0);
        createdWindow.FrameType.Should().Be("Vinyl");
        createdWindow.GlazingType.Should().Be("Double Pane");
        createdWindow.UValue.Should().Be(0.3);
        createdWindow.HasScreens.Should().Be(true);
        createdWindow.HasStormWindows.Should().Be(false);
        createdWindow.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain($"/api/windows/{createdWindow.Id}".ToLowerInvariant());
    }

    [Fact]
    public async Task POST_Windows_Should_Return_400_For_Invalid_Data()
    {
        // Arrange
        
        var invalidRequest = new 
        { 
            Name = "", // Empty name - should fail validation
            Width = 1.5,
            Height = 2.0,
            Area = 3.0,
            FrameType = "Vinyl",
            GlazingType = "Double Pane"
        };

        // Act
        var response = await PostJsonAsync("/api/windows", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Windows_Should_Validate_Numeric_Ranges()
    {
        // Arrange
        
        var invalidRequest = TestDataBuilders.CreateWindowRequest(
            name: "Test Window",
            width: -1.0, // Invalid - should be > 0.1
            height: 2.0,
            area: 3.0,
            frameType: "Vinyl",
            glazingType: "Double Pane"
        );

        // Act
        var response = await PostJsonAsync("/api/windows", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Windows_Should_Return_All_Windows()
    {
        // Arrange
        
        
        // Create test windows
        var window1 = TestDataBuilders.CreateWindowRequest("Window 1", "First window", 1.2, 1.8, 2.16, "Vinyl", "Single Pane");
        var window2 = TestDataBuilders.CreateWindowRequest("Window 2", "Second window", 1.0, 1.5, 1.5, "Wood", "Triple Pane");
        
        await PostJsonAsync("/api/windows", window1);
        await PostJsonAsync("/api/windows", window2);

        // Act
        var response = await Client.GetAsync("/api/windows");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var windows = await ReadJsonAsync<List<WindowResponse>>(response);
        
        windows.Should().NotBeNull();
        windows.Should().HaveCount(2);
        windows.Should().Contain(w => w.Name == "Window 1");
        windows.Should().Contain(w => w.Name == "Window 2");
    }

    [Fact]
    public async Task GET_Window_By_Id_Should_Return_Window_When_Exists()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest("Test Window", "Test description", 1.5, 2.0, 3.0, "Aluminum", "Double Pane");
        var createResponse = await PostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);

        // Act
        var response = await Client.GetAsync($"/api/windows/{createdWindow!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var window = await ReadJsonAsync<WindowResponse>(response);
        
        window.Should().NotBeNull();
        window!.Id.Should().Be(createdWindow.Id);
        window.Name.Should().Be("Test Window");
        window.Description.Should().Be("Test description");
        window.Width.Should().Be(1.5);
        window.Height.Should().Be(2.0);
        window.Area.Should().Be(3.0);
        window.FrameType.Should().Be("Aluminum");
        window.GlazingType.Should().Be("Double Pane");
    }

    [Fact]
    public async Task GET_Window_By_Id_Should_Return_404_When_Not_Exists()
    {
        // Arrange
        
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/windows/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Window_Should_Update_Existing_Window()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest("Original Window", "Original description", 1.0, 1.5, 1.5, "Wood", "Single Pane");
        var createResponse = await PostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);

        var updateRequest = TestDataBuilders.UpdateWindowRequest(
            name: "Updated Window",
            description: "Updated description",
            width: 2.0,
            height: 2.5,
            area: 5.0,
            frameType: "Vinyl",
            glazingType: "Triple Pane",
            uValue: 0.2,
            solarHeatGainCoefficient: 0.3,
            hasScreens: true,
            hasStormWindows: true
        );

        // Act
        var response = await PutJsonAsync($"/api/windows/{createdWindow!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/windows/{createdWindow.Id}");
        var updatedWindow = await ReadJsonAsync<WindowResponse>(getResponse);
        
        updatedWindow.Should().NotBeNull();
        updatedWindow!.Name.Should().Be("Updated Window");
        updatedWindow.Description.Should().Be("Updated description");
        updatedWindow.Width.Should().Be(2.0);
        updatedWindow.Height.Should().Be(2.5);
        updatedWindow.Area.Should().Be(5.0);
        updatedWindow.FrameType.Should().Be("Vinyl");
        updatedWindow.GlazingType.Should().Be("Triple Pane");
        updatedWindow.UValue.Should().BeApproximately(0.2, 0.0001);
        updatedWindow.SolarHeatGainCoefficient.Should().BeApproximately(0.3, 0.0001);
        updatedWindow.HasScreens.Should().Be(true);
        updatedWindow.HasStormWindows.Should().Be(true);
        updatedWindow.UpdatedAt.Should().NotBeNull();
        updatedWindow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PUT_Window_Should_Return_404_When_Not_Exists()
    {
        // Arrange
        
        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdateWindowRequest("Updated Window", "Updated description", 2.0, 2.5, 5.0, "Vinyl", "Triple Pane");

        // Act
        var response = await PutJsonAsync($"/api/windows/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Window_Should_Remove_Existing_Window()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest("To Delete", "Window to be deleted", 1.0, 1.5, 1.5, "Wood", "Single Pane");
        var createResponse = await PostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);

        // Act
        var response = await Client.DeleteAsync($"/api/windows/{createdWindow!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the window is deleted
        var getResponse = await Client.GetAsync($"/api/windows/{createdWindow.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Window_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/windows/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Window_Should_Handle_Optional_Fields()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest(
            name: "Simple Window",
            description: null,
            width: 1.0,
            height: 1.5,
            area: 1.5,
            frameType: "Wood",
            frameDetails: null,
            glazingType: "Single Pane",
            glazingDetails: null,
            uValue: null,
            solarHeatGainCoefficient: null,
            visibleTransmittance: null,
            airLeakage: null,
            energyStarRating: null,
            nfrcRating: null,
            orientation: null,
            location: null,
            installationType: null,
            operationType: null,
            hasScreens: null,
            hasStormWindows: null
        );

        // Act
        var response = await PostJsonAsync("/api/windows", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWindow = await ReadJsonAsync<WindowResponse>(response);
        
        createdWindow.Should().NotBeNull();
        createdWindow!.Name.Should().Be("Simple Window");
        createdWindow.Description.Should().BeNull();
        createdWindow.FrameDetails.Should().BeNull();
        createdWindow.GlazingDetails.Should().BeNull();
        createdWindow.UValue.Should().BeNull();
        createdWindow.SolarHeatGainCoefficient.Should().BeNull();
        createdWindow.VisibleTransmittance.Should().BeNull();
        createdWindow.AirLeakage.Should().BeNull();
        createdWindow.EnergyStarRating.Should().BeNull();
        createdWindow.NFRCRating.Should().BeNull();
        createdWindow.Orientation.Should().BeNull();
        createdWindow.Location.Should().BeNull();
        createdWindow.InstallationType.Should().BeNull();
        createdWindow.OperationType.Should().BeNull();
        createdWindow.HasScreens.Should().BeNull();
        createdWindow.HasStormWindows.Should().BeNull();
    }

    [Fact]
    public async Task Windows_Should_Persist_Energy_Efficiency_Data()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest(
            name: "Energy Efficient Window",
            description: "High-performance window",
            width: 1.2,
            height: 1.8,
            area: 2.16,
            frameType: "Fiberglass",
            frameDetails: "Insulated fiberglass frame",
            glazingType: "Triple Pane",
            glazingDetails: "Low-E coating, krypton fill",
            uValue: 0.15,
            solarHeatGainCoefficient: 0.2,
            visibleTransmittance: 0.65,
            airLeakage: 0.05,
            energyStarRating: "Most Efficient",
            nfrcRating: "A++",
            orientation: "North",
            location: "Bedroom",
            installationType: "Replacement",
            operationType: "Casement",
            hasScreens: true,
            hasStormWindows: false
        );

        // Act
        var response = await PostJsonAsync("/api/windows", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWindow = await ReadJsonAsync<WindowResponse>(response);
        
        createdWindow.Should().NotBeNull();
        createdWindow!.UValue.Should().Be(0.15);
        createdWindow.SolarHeatGainCoefficient.Should().Be(0.2);
        createdWindow.VisibleTransmittance.Should().Be(0.65);
        createdWindow.AirLeakage.Should().Be(0.05);
        createdWindow.EnergyStarRating.Should().Be("Most Efficient");
        createdWindow.NFRCRating.Should().Be("A++");
        createdWindow.FrameDetails.Should().Be("Insulated fiberglass frame");
        createdWindow.GlazingDetails.Should().Be("Low-E coating, krypton fill");
    }

    [Fact]
    public async Task Windows_Should_Handle_Boolean_Properties_Correctly()
    {
        // Arrange
        
        
        // Test with true values
        var createRequest1 = TestDataBuilders.CreateWindowRequest(
            name: "Window with Screens and Storms",
            width: 1.0,
            height: 1.5,
            area: 1.5,
            frameType: "Wood",
            glazingType: "Double Pane",
            hasScreens: true,
            hasStormWindows: true
        );

        // Test with false values
        var createRequest2 = TestDataBuilders.CreateWindowRequest(
            name: "Window without Screens and Storms",
            width: 1.0,
            height: 1.5,
            area: 1.5,
            frameType: "Vinyl",
            glazingType: "Single Pane",
            hasScreens: false,
            hasStormWindows: false
        );

        // Act
        var response1 = await PostJsonAsync("/api/windows", createRequest1);
        var response2 = await PostJsonAsync("/api/windows", createRequest2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var window1 = await ReadJsonAsync<WindowResponse>(response1);
        var window2 = await ReadJsonAsync<WindowResponse>(response2);

        window1!.HasScreens.Should().Be(true);
        window1.HasStormWindows.Should().Be(true);
        
        window2!.HasScreens.Should().Be(false);
        window2.HasStormWindows.Should().Be(false);
    }

    [Fact]
    public async Task Windows_Should_Persist_Timestamps_Correctly()
    {
        // Arrange
        
        var createRequest = TestDataBuilders.CreateWindowRequest("Timestamp Test", "Test timestamps", 1.0, 1.5, 1.5, "Wood", "Double Pane");

        // Act - Create window
        var createResponse = await PostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);
        var createdAt = createdWindow!.CreatedAt;

        // Wait a moment to ensure different timestamps
        await Task.Delay(100);

        // Act - Update window
        var updateRequest = TestDataBuilders.UpdateWindowRequest("Updated Timestamp Test", "Updated timestamps", 1.2, 1.8, 2.16, "Vinyl", "Triple Pane");
        await PutJsonAsync($"/api/windows/{createdWindow.Id}", updateRequest);

        // Act - Get updated window
        var getResponse = await Client.GetAsync($"/api/windows/{createdWindow.Id}");
        var updatedWindow = await ReadJsonAsync<WindowResponse>(getResponse);

        // Assert
        updatedWindow.Should().NotBeNull();
        updatedWindow!.CreatedAt.Should().Be(createdAt); // CreatedAt should not change
        updatedWindow.UpdatedAt.Should().NotBeNull();
        updatedWindow.UpdatedAt.Should().BeAfter(createdAt); // UpdatedAt should be after CreatedAt
    }
}

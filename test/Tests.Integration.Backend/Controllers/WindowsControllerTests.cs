using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.Controllers;

public class WindowsControllerTests : IntegrationTestBase
{
    public WindowsControllerTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_Windows_Should_Return_Empty_List_Initially()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange


            // Act
            var response = await AuthenticatedGetAsync("/api/windows");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var windows = await ReadJsonAsync<List<WindowResponse>>(response);
            Assert.NotNull(windows);
            Assert.Empty(windows);
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
        var response = await AuthenticatedPostJsonAsync("/api/windows", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdWindow = await ReadJsonAsync<WindowResponse>(response);

        Assert.NotNull(createdWindow);
        Assert.NotEqual(Guid.Empty, createdWindow!.Id);
        Assert.Equal("Living Room Window", createdWindow.Name);
        Assert.Equal("Large south-facing window", createdWindow.Description);
        Assert.Equal(1.5, createdWindow.Width);
        Assert.Equal(2.0, createdWindow.Height);
        Assert.Equal(3.0, createdWindow.Area);
        Assert.Equal("Vinyl", createdWindow.FrameType);
        Assert.Equal("Double Pane", createdWindow.GlazingType);
        Assert.Equal(0.3, createdWindow.UValue);
        Assert.True(createdWindow.HasScreens);
        Assert.False(createdWindow.HasStormWindows);
        Assert.True((DateTime.UtcNow - createdWindow.CreatedAt).TotalMinutes < 1);

        // Verify location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/windows/{createdWindow.Id}".ToLowerInvariant(), response.Headers.Location!.ToString().ToLowerInvariant());
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
        var response = await AuthenticatedPostJsonAsync("/api/windows", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var response = await AuthenticatedPostJsonAsync("/api/windows", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Windows_Should_Return_All_Windows()
    {
        // Arrange


        // Create test windows
        var window1 = TestDataBuilders.CreateWindowRequest("Window 1", "First window", 1.2, 1.8, 2.16, "Vinyl", "Single Pane");
        var window2 = TestDataBuilders.CreateWindowRequest("Window 2", "Second window", 1.0, 1.5, 1.5, "Wood", "Triple Pane");

        await AuthenticatedPostJsonAsync("/api/windows", window1);
        await AuthenticatedPostJsonAsync("/api/windows", window2);

        // Act
        var response = await AuthenticatedGetAsync("/api/windows");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var windows = await ReadJsonAsync<List<WindowResponse>>(response);

        Assert.NotNull(windows);
        Assert.Equal(2, windows.Count);
        Assert.Contains(windows, w => w.Name == "Window 1");
        Assert.Contains(windows, w => w.Name == "Window 2");
    }

    [Fact]
    public async Task GET_Window_By_Id_Should_Return_Window_When_Exists()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWindowRequest("Test Window", "Test description", 1.5, 2.0, 3.0, "Aluminum", "Double Pane");
        var createResponse = await AuthenticatedPostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);

        // Act
        var response = await AuthenticatedGetAsync($"/api/windows/{createdWindow!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var window = await ReadJsonAsync<WindowResponse>(response);

        Assert.NotNull(window);
        Assert.Equal(createdWindow.Id, window!.Id);
        Assert.Equal("Test Window", window.Name);
        Assert.Equal("Test description", window.Description);
        Assert.Equal(1.5, window.Width);
        Assert.Equal(2.0, window.Height);
        Assert.Equal(3.0, window.Area);
        Assert.Equal("Aluminum", window.FrameType);
        Assert.Equal("Double Pane", window.GlazingType);
    }

    [Fact]
    public async Task GET_Window_By_Id_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await AuthenticatedGetAsync($"/api/windows/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Window_Should_Update_Existing_Window()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWindowRequest("Original Window", "Original description", 1.0, 1.5, 1.5, "Wood", "Single Pane");
        var createResponse = await AuthenticatedPostJsonAsync("/api/windows", createRequest);
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
        var response = await AuthenticatedPutJsonAsync($"/api/windows/{createdWindow!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await AuthenticatedGetAsync($"/api/windows/{createdWindow.Id}");
        var updatedWindow = await ReadJsonAsync<WindowResponse>(getResponse);

        Assert.NotNull(updatedWindow);
        Assert.Equal("Updated Window", updatedWindow!.Name);
        Assert.Equal("Updated description", updatedWindow.Description);
        Assert.Equal(2.0, updatedWindow.Width);
        Assert.Equal(2.5, updatedWindow.Height);
        Assert.Equal(5.0, updatedWindow.Area);
        Assert.Equal("Vinyl", updatedWindow.FrameType);
        Assert.Equal("Triple Pane", updatedWindow.GlazingType);
        Assert.Equal(0.2, updatedWindow.UValue!.Value, 4);
        Assert.Equal(0.3, updatedWindow.SolarHeatGainCoefficient!.Value, 4);
        Assert.True(updatedWindow.HasScreens);
        Assert.True(updatedWindow.HasStormWindows);
        Assert.NotNull(updatedWindow.UpdatedAt);
        Assert.True((DateTime.UtcNow - updatedWindow.UpdatedAt.Value).TotalMinutes < 1);
    }

    [Fact]
    public async Task PUT_Window_Should_Return_404_When_Not_Exists()
    {
        // Arrange

        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilders.UpdateWindowRequest("Updated Window", "Updated description", 2.0, 2.5, 5.0, "Vinyl", "Triple Pane");

        // Act
        var response = await AuthenticatedPutJsonAsync($"/api/windows/{nonExistentId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Window_Should_Remove_Existing_Window()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWindowRequest("To Delete", "Window to be deleted", 1.0, 1.5, 1.5, "Wood", "Single Pane");
        var createResponse = await AuthenticatedPostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);

        // Act
        var response = await AuthenticatedDeleteAsync($"/api/windows/{createdWindow!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the window is deleted
        var getResponse = await AuthenticatedGetAsync($"/api/windows/{createdWindow.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DELETE_Window_Should_Return_204_When_Not_Exists()
    {
        // Arrange - DELETE is idempotent, returns 204 even for non-existent resources
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await AuthenticatedDeleteAsync($"/api/windows/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
        var response = await AuthenticatedPostJsonAsync("/api/windows", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdWindow = await ReadJsonAsync<WindowResponse>(response);

        Assert.NotNull(createdWindow);
        Assert.Equal("Simple Window", createdWindow!.Name);
        Assert.Null(createdWindow.Description);
        Assert.Null(createdWindow.FrameDetails);
        Assert.Null(createdWindow.GlazingDetails);
        Assert.Null(createdWindow.UValue);
        Assert.Null(createdWindow.SolarHeatGainCoefficient);
        Assert.Null(createdWindow.VisibleTransmittance);
        Assert.Null(createdWindow.AirLeakage);
        Assert.Null(createdWindow.EnergyStarRating);
        Assert.Null(createdWindow.NFRCRating);
        Assert.Null(createdWindow.Orientation);
        Assert.Null(createdWindow.Location);
        Assert.Null(createdWindow.InstallationType);
        Assert.Null(createdWindow.OperationType);
        Assert.Null(createdWindow.HasScreens);
        Assert.Null(createdWindow.HasStormWindows);
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
        var response = await AuthenticatedPostJsonAsync("/api/windows", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdWindow = await ReadJsonAsync<WindowResponse>(response);

        Assert.NotNull(createdWindow);
        Assert.Equal(0.15, createdWindow!.UValue);
        Assert.Equal(0.2, createdWindow.SolarHeatGainCoefficient);
        Assert.Equal(0.65, createdWindow.VisibleTransmittance);
        Assert.Equal(0.05, createdWindow.AirLeakage);
        Assert.Equal("Most Efficient", createdWindow.EnergyStarRating);
        Assert.Equal("A++", createdWindow.NFRCRating);
        Assert.Equal("Insulated fiberglass frame", createdWindow.FrameDetails);
        Assert.Equal("Low-E coating, krypton fill", createdWindow.GlazingDetails);
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
        var response1 = await AuthenticatedPostJsonAsync("/api/windows", createRequest1);
        var response2 = await AuthenticatedPostJsonAsync("/api/windows", createRequest2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var window1 = await ReadJsonAsync<WindowResponse>(response1);
        var window2 = await ReadJsonAsync<WindowResponse>(response2);

        Assert.True(window1!.HasScreens);
        Assert.True(window1.HasStormWindows);

        Assert.False(window2!.HasScreens);
        Assert.False(window2.HasStormWindows);
    }

    [Fact]
    public async Task Windows_Should_Persist_Timestamps_Correctly()
    {
        // Arrange

        var createRequest = TestDataBuilders.CreateWindowRequest("Timestamp Test", "Test timestamps", 1.0, 1.5, 1.5, "Wood", "Double Pane");

        // Act - Create window
        var createResponse = await AuthenticatedPostJsonAsync("/api/windows", createRequest);
        var createdWindow = await ReadJsonAsync<WindowResponse>(createResponse);
        var createdAt = createdWindow!.CreatedAt;

        // Wait a moment to ensure different timestamps
        await Task.Delay(100);

        // Act - Update window
        var updateRequest = TestDataBuilders.UpdateWindowRequest("Updated Timestamp Test", "Updated timestamps", 1.2, 1.8, 2.16, "Vinyl", "Triple Pane");
        await AuthenticatedPutJsonAsync($"/api/windows/{createdWindow.Id}", updateRequest);

        // Act - Get updated window
        var getResponse = await AuthenticatedGetAsync($"/api/windows/{createdWindow.Id}");
        var updatedWindow = await ReadJsonAsync<WindowResponse>(getResponse);

        // Assert
        Assert.NotNull(updatedWindow);
        Assert.Equal(createdAt, updatedWindow!.CreatedAt); // CreatedAt should not change
        Assert.NotNull(updatedWindow.UpdatedAt);
        Assert.True(updatedWindow.UpdatedAt > createdAt); // UpdatedAt should be after CreatedAt
    }
}

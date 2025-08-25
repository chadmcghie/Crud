using App.Abstractions;
using App.Services;
using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Application;

public class WindowServiceTests
{
    private readonly Mock<IWindowRepository> _mockWindowRepository;
    private readonly WindowService _windowService;

    public WindowServiceTests()
    {
        _mockWindowRepository = new Mock<IWindowRepository>();
        _windowService = new WindowService(_mockWindowRepository.Object);
    }

    public class GetAsync : WindowServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldReturnWindow()
        {
            // Arrange
            var windowId = Guid.NewGuid();
            var expectedWindow = WindowTestDataBuilder.Default().Build();
            expectedWindow.Id = windowId;

            _mockWindowRepository.Setup(x => x.GetAsync(windowId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWindow);

            // Act
            var result = await _windowService.GetAsync(windowId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedWindow);
            _mockWindowRepository.Verify(x => x.GetAsync(windowId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var windowId = Guid.NewGuid();
            _mockWindowRepository.Setup(x => x.GetAsync(windowId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Window?)null);

            // Act
            var result = await _windowService.GetAsync(windowId);

            // Assert
            result.Should().BeNull();
        }
    }

    public class ListAsync : WindowServiceTests
    {
        [Fact]
        public async Task ShouldReturnAllWindows()
        {
            // Arrange
            var windows = new List<Window>
            {
                WindowTestDataBuilder.Default().WithName("Window 1").Build(),
                WindowTestDataBuilder.Default().WithName("Window 2").Build()
            };

            _mockWindowRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(windows);

            // Act
            var result = await _windowService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(windows);
        }

        [Fact]
        public async Task WithEmptyRepository_ShouldReturnEmptyList()
        {
            // Arrange
            _mockWindowRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Window>());

            // Act
            var result = await _windowService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    public class CreateAsync : WindowServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldCreateWindow()
        {
            // Arrange
            var name = "Test Window";
            var description = "Test Description";
            var width = 3.0;
            var height = 4.0;
            var area = 12.0;
            var frameType = "Vinyl";
            var frameDetails = "White vinyl frame";
            var glazingType = "Double";
            var glazingDetails = "Low-E coating";
            var uValue = 0.30;
            var shgc = 0.25;
            var vt = 0.70;
            var airLeakage = 0.1;
            var energyStar = "Yes";
            var nfrc = "NFRC-123";
            var orientation = "South";
            var location = "Living Room";
            var installation = "New";
            var operation = "Casement";
            var hasScreens = true;
            var hasStorm = false;

            var expectedWindow = WindowTestDataBuilder.Default().Build();
            _mockWindowRepository.Setup(x => x.AddAsync(It.IsAny<Window>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWindow);

            // Act
            var result = await _windowService.CreateAsync(name, description, width, height, area,
                frameType, frameDetails, glazingType, glazingDetails, uValue, shgc, vt, airLeakage,
                energyStar, nfrc, orientation, location, installation, operation, hasScreens, hasStorm);

            // Assert
            result.Should().NotBeNull();
            _mockWindowRepository.Verify(x => x.AddAsync(It.Is<Window>(w => 
                w.Name == name && 
                w.Description == description &&
                w.Width == width &&
                w.Height == height &&
                w.Area == area &&
                w.FrameType == frameType &&
                w.FrameDetails == frameDetails &&
                w.GlazingType == glazingType &&
                w.GlazingDetails == glazingDetails &&
                w.UValue == uValue &&
                w.SolarHeatGainCoefficient == shgc &&
                w.VisibleTransmittance == vt &&
                w.AirLeakage == airLeakage &&
                w.EnergyStarRating == energyStar &&
                w.NFRCRating == nfrc &&
                w.Orientation == orientation &&
                w.Location == location &&
                w.InstallationType == installation &&
                w.OperationType == operation &&
                w.HasScreens == hasScreens &&
                w.HasStormWindows == hasStorm), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithMinimalData_ShouldCreateWindow()
        {
            // Arrange
            var name = "Simple Window";
            var width = 2.0;
            var height = 3.0;
            var area = 6.0;
            var frameType = "Wood";
            var glazingType = "Single";

            var expectedWindow = WindowTestDataBuilder.Default().Build();
            _mockWindowRepository.Setup(x => x.AddAsync(It.IsAny<Window>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWindow);

            // Act
            var result = await _windowService.CreateAsync(name, null, width, height, area, frameType, null, glazingType, null);

            // Assert
            result.Should().NotBeNull();
            _mockWindowRepository.Verify(x => x.AddAsync(It.Is<Window>(w => 
                w.Name == name && 
                w.Description == null &&
                w.Width == width &&
                w.Height == height &&
                w.Area == area &&
                w.FrameType == frameType &&
                w.GlazingType == glazingType), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class UpdateAsync : WindowServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldUpdateWindow()
        {
            // Arrange
            var windowId = Guid.NewGuid();
            var existingWindow = WindowTestDataBuilder.Default().Build();
            existingWindow.Id = windowId;
            existingWindow.UpdatedAt = null;

            var newName = "Updated Window";
            var newDescription = "Updated Description";
            var newWidth = 4.0;
            var newHeight = 5.0;
            var newArea = 20.0;
            var newFrameType = "Aluminum";
            var newGlazingType = "Triple";

            _mockWindowRepository.Setup(x => x.GetAsync(windowId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWindow);

            // Act
            await _windowService.UpdateAsync(windowId, newName, newDescription, newWidth, newHeight, 
                newArea, newFrameType, null, newGlazingType, null);

            // Assert
            existingWindow.Name.Should().Be(newName);
            existingWindow.Description.Should().Be(newDescription);
            existingWindow.Width.Should().Be(newWidth);
            existingWindow.Height.Should().Be(newHeight);
            existingWindow.Area.Should().Be(newArea);
            existingWindow.FrameType.Should().Be(newFrameType);
            existingWindow.GlazingType.Should().Be(newGlazingType);
            existingWindow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

            _mockWindowRepository.Verify(x => x.UpdateAsync(existingWindow, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidWindowId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidWindowId = Guid.NewGuid();
            _mockWindowRepository.Setup(x => x.GetAsync(invalidWindowId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Window?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _windowService.UpdateAsync(invalidWindowId, "Name", "Desc", 1, 1, 1, "Frame", null, "Glazing", null));

            exception.Message.Should().Contain($"Window {invalidWindowId} not found");
        }
    }

    public class DeleteAsync : WindowServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldDeleteWindow()
        {
            // Arrange
            var windowId = Guid.NewGuid();

            // Act
            await _windowService.DeleteAsync(windowId);

            // Assert
            _mockWindowRepository.Verify(x => x.DeleteAsync(windowId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPassCancellationToken()
        {
            // Arrange
            var windowId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            // Act
            await _windowService.DeleteAsync(windowId, cancellationToken);

            // Assert
            _mockWindowRepository.Verify(x => x.DeleteAsync(windowId, cancellationToken), Times.Once);
        }
    }
}


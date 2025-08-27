using App.Abstractions;
using App.Services;
using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Application;

public class WallServiceTests
{
    private readonly Mock<IWallRepository> _mockWallRepository;
    private readonly WallService _wallService;

    public WallServiceTests()
    {
        _mockWallRepository = new Mock<IWallRepository>();
        _wallService = new WallService(_mockWallRepository.Object);
    }

    public class GetAsync : WallServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldReturnWall()
        {
            // Arrange
            var wallId = Guid.NewGuid();
            var expectedWall = WallTestDataBuilder.Default().Build();
            expectedWall.Id = wallId;

            _mockWallRepository.Setup(x => x.GetAsync(wallId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWall);

            // Act
            var result = await _wallService.GetAsync(wallId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedWall);
            _mockWallRepository.Verify(x => x.GetAsync(wallId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var wallId = Guid.NewGuid();
            _mockWallRepository.Setup(x => x.GetAsync(wallId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Wall?)null);

            // Act
            var result = await _wallService.GetAsync(wallId);

            // Assert
            result.Should().BeNull();
        }
    }

    public class ListAsync : WallServiceTests
    {
        [Fact]
        public async Task ShouldReturnAllWalls()
        {
            // Arrange
            var walls = new List<Wall>
            {
                WallTestDataBuilder.Default().WithName("Wall 1").Build(),
                WallTestDataBuilder.Default().WithName("Wall 2").Build()
            };

            _mockWallRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(walls);

            // Act
            var result = await _wallService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(walls);
        }

        [Fact]
        public async Task WithEmptyRepository_ShouldReturnEmptyList()
        {
            // Arrange
            _mockWallRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Wall>());

            // Act
            var result = await _wallService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    public class CreateAsync : WallServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldCreateWall()
        {
            // Arrange
            var name = "Test Wall";
            var description = "Test Description";
            var length = 10.0;
            var height = 9.0;
            var thickness = 6.0;
            var assemblyType = "2x4 16\" OC";
            var assemblyDetails = "Standard construction";
            var rValue = 13.0;
            var uValue = 0.077;
            var materialLayers = "Drywall, Insulation, Sheathing";
            var orientation = "North";
            var location = "Exterior";

            var expectedWall = WallTestDataBuilder.Default()
                .WithName(name)
                .WithDescription(description)
                .WithDimensions(length, height, thickness)
                .WithAssemblyType(assemblyType)
                .WithThermalProperties(rValue, uValue)
                .Build();

            _mockWallRepository.Setup(x => x.AddAsync(It.IsAny<Wall>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWall);

            // Act
            var result = await _wallService.CreateAsync(name, description, length, height, thickness, 
                assemblyType, assemblyDetails, rValue, uValue, materialLayers, orientation, location);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(name);
            result.Description.Should().Be(description);
            result.Length.Should().Be(length);
            result.Height.Should().Be(height);
            result.Thickness.Should().Be(thickness);
            result.AssemblyType.Should().Be(assemblyType);

            _mockWallRepository.Verify(x => x.AddAsync(It.Is<Wall>(w => 
                w.Name == name && 
                w.Description == description &&
                w.Length == length &&
                w.Height == height &&
                w.Thickness == thickness &&
                w.AssemblyType == assemblyType &&
                w.AssemblyDetails == assemblyDetails &&
                w.RValue == rValue &&
                w.UValue == uValue &&
                w.MaterialLayers == materialLayers &&
                w.Orientation == orientation &&
                w.Location == location), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithMinimalData_ShouldCreateWall()
        {
            // Arrange
            var name = "Simple Wall";
            var length = 8.0;
            var height = 8.0;
            var thickness = 4.0;
            var assemblyType = "Basic assembly";

            var expectedWall = WallTestDataBuilder.Default().Build();
            _mockWallRepository.Setup(x => x.AddAsync(It.IsAny<Wall>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWall);

            // Act
            var result = await _wallService.CreateAsync(name, null, length, height, thickness, assemblyType);

            // Assert
            result.Should().NotBeNull();
            _mockWallRepository.Verify(x => x.AddAsync(It.Is<Wall>(w => 
                w.Name == name && 
                w.Description == null &&
                w.Length == length &&
                w.Height == height &&
                w.Thickness == thickness &&
                w.AssemblyType == assemblyType), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class UpdateAsync : WallServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldUpdateWall()
        {
            // Arrange
            var wallId = Guid.NewGuid();
            var existingWall = WallTestDataBuilder.Default().Build();
            existingWall.Id = wallId;
            existingWall.UpdatedAt = null;

            var newName = "Updated Wall";
            var newDescription = "Updated Description";
            var newLength = 12.0;
            var newHeight = 10.0;
            var newThickness = 8.0;
            var newAssemblyType = "Updated Assembly";

            _mockWallRepository.Setup(x => x.GetAsync(wallId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWall);

            // Act
            await _wallService.UpdateAsync(wallId, newName, newDescription, newLength, newHeight, 
                newThickness, newAssemblyType);

            // Assert
            existingWall.Name.Should().Be(newName);
            existingWall.Description.Should().Be(newDescription);
            existingWall.Length.Should().Be(newLength);
            existingWall.Height.Should().Be(newHeight);
            existingWall.Thickness.Should().Be(newThickness);
            existingWall.AssemblyType.Should().Be(newAssemblyType);
            existingWall.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

            _mockWallRepository.Verify(x => x.UpdateAsync(existingWall, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidWallId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidWallId = Guid.NewGuid();
            _mockWallRepository.Setup(x => x.GetAsync(invalidWallId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Wall?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _wallService.UpdateAsync(invalidWallId, "Name", "Desc", 1, 1, 1, "Assembly"));

            exception.Message.Should().Contain($"Wall {invalidWallId} not found");
        }
    }

    public class DeleteAsync : WallServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldDeleteWall()
        {
            // Arrange
            var wallId = Guid.NewGuid();

            // Act
            await _wallService.DeleteAsync(wallId);

            // Assert
            _mockWallRepository.Verify(x => x.DeleteAsync(wallId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPassCancellationToken()
        {
            // Arrange
            var wallId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            // Act
            await _wallService.DeleteAsync(wallId, cancellationToken);

            // Assert
            _mockWallRepository.Verify(x => x.DeleteAsync(wallId, cancellationToken), Times.Once);
        }
    }
}








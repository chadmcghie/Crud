using Domain.Entities;
using Domain.Exceptions;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Domain;

public class WallTests
{
    public class Construction : WallTests
    {
        [Fact]
        public void WithValidData_ShouldCreateWall()
        {
            // Arrange & Act
            var wall = WallTestDataBuilder.Default()
                .WithName("Exterior Wall")
                .WithDimensions(10.0, 9.0, 6.0)
                .WithAssemblyType("2x4 16\" on center")
                .WithThermalProperties(13.0, 0.077)
                .Build();

            // Assert
            Assert.NotNull(wall);
            Assert.NotEqual(Guid.Empty, wall.Id);
            Assert.Equal("Exterior Wall", wall.Name);
            Assert.Equal(10.0, wall.Length);
            Assert.Equal(9.0, wall.Height);
            Assert.Equal(6.0, wall.Thickness);
            Assert.Equal("2x4 16\" on center", wall.AssemblyType);
            Assert.Equal(13.0, wall.RValue);
            Assert.Equal(0.077, wall.UValue);
            Assert.True((DateTime.UtcNow - wall.CreatedAt).TotalSeconds < 1);
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var wall1 = WallTestDataBuilder.Default().Build();
            var wall2 = WallTestDataBuilder.Default().Build();

            // Assert
            Assert.NotEqual(wall2.Id, wall1.Id);
        }

        [Fact]
        public void ShouldSetCreatedAtToCurrentTime()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var wall = WallTestDataBuilder.Default().Build();

            // Assert
            var afterCreation = DateTime.UtcNow;
            Assert.True(wall.CreatedAt >= beforeCreation);
            Assert.True(wall.CreatedAt <= afterCreation);
        }
    }

    public class Properties : WallTests
    {
        [Fact]
        public void WithOptionalProperties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var wall = WallTestDataBuilder.Default()
                .WithDescription("Test wall description")
                .Build();

            // Assert
            Assert.Equal("Test wall description", wall.Description);
            // UpdatedAt should have a value because the domain method was called to set additional properties
            Assert.NotNull(wall.UpdatedAt);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void WithInvalidDimensions_ShouldThrowDomainException(double invalidValue)
        {
            // The domain entity now enforces business rules and should throw for invalid dimensions

            // Arrange & Act & Assert
            var ex = Assert.Throws<DomainException>(() => WallTestDataBuilder.Default()
                .WithDimensions(invalidValue, 9.0, 6.0)
                .Build());

            Assert.Contains("must be greater than zero", ex.Message);
        }
    }
}


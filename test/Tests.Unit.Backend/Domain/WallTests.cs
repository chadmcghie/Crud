using Domain.Entities;
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
            wall.Should().NotBeNull();
            wall.Id.Should().NotBeEmpty();
            wall.Name.Should().Be("Exterior Wall");
            wall.Length.Should().Be(10.0);
            wall.Height.Should().Be(9.0);
            wall.Thickness.Should().Be(6.0);
            wall.AssemblyType.Should().Be("2x4 16\" on center");
            wall.RValue.Should().Be(13.0);
            wall.UValue.Should().Be(0.077);
            wall.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var wall1 = WallTestDataBuilder.Default().Build();
            var wall2 = WallTestDataBuilder.Default().Build();

            // Assert
            wall1.Id.Should().NotBe(wall2.Id);
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
            wall.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            wall.CreatedAt.Should().BeOnOrBefore(afterCreation);
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
            wall.Description.Should().Be("Test wall description");
            wall.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void WithInvalidDimensions_ShouldStillCreateWall_ValidationHandledByDataAnnotations(double invalidValue)
        {
            // Note: Entity creation doesn't enforce business rule validation
            // This test documents current behavior
            
            // Arrange & Act
            var wall = WallTestDataBuilder.Default()
                .WithDimensions(invalidValue, 9.0, 6.0)
                .Build();

            // Assert
            wall.Should().NotBeNull();
            wall.Length.Should().Be(invalidValue);
        }
    }
}

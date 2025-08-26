using Domain.Entities;
using Xunit;

namespace Tests.Unit.Backend.Domain.Entities
{
    public class WallTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateWall()
        {
            // Arrange
            var name = "External Wall";
            var assemblyType = "2x4 16\" on center with R13 fiberglass insulation";

            // Act
            var wall = new Wall(name, assemblyType);

            // Assert
            Assert.Equal(name, wall.Name);
            Assert.Equal(assemblyType, wall.AssemblyType);
            Assert.NotEqual(Guid.Empty, wall.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Wall(invalidName, "Assembly Type"));
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Wall(null!, "Assembly Type"));
        }

        [Fact]
        public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longName = new string('a', 201); // Over the 200 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Wall(longName, "Assembly Type"));
        }

        [Fact]
        public void Constructor_WithAssemblyTypeTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longAssemblyType = new string('a', 501); // Over the 500 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Wall("Name", longAssemblyType));
        }

        [Fact]
        public void SetDescription_WithDescriptionTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var wall = new Wall("Name", "Assembly Type");
            var longDescription = new string('a', 1001); // Over the 1000 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => wall.Description = longDescription);
        }
    }
}
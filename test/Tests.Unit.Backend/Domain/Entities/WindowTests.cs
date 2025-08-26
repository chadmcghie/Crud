using Domain.Entities;
using Xunit;

namespace Tests.Unit.Backend.Domain.Entities
{
    public class WindowTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateWindow()
        {
            // Arrange
            var name = "Front Window";
            var frameType = "Vinyl";
            var glazingType = "Double";

            // Act
            var window = new Window(name, frameType, glazingType);

            // Assert
            Assert.Equal(name, window.Name);
            Assert.Equal(frameType, window.FrameType);
            Assert.Equal(glazingType, window.GlazingType);
            Assert.NotEqual(Guid.Empty, window.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Window(invalidName, "Vinyl", "Double"));
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Window(null!, "Vinyl", "Double"));
        }

        [Fact]
        public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longName = new string('a', 201); // Over the 200 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Window(longName, "Vinyl", "Double"));
        }

        [Fact]
        public void Constructor_WithFrameTypeTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longFrameType = new string('a', 101); // Over the 100 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Window("Name", longFrameType, "Double"));
        }

        [Fact]
        public void Constructor_WithGlazingTypeTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longGlazingType = new string('a', 101); // Over the 100 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Window("Name", "Vinyl", longGlazingType));
        }

        [Fact]
        public void SetEnergyStarRating_WithRatingTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var window = new Window("Name", "Vinyl", "Double");
            var longRating = new string('a', 51); // Over the 50 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => window.EnergyStarRating = longRating);
        }
    }
}
using Domain.Entities;
using Xunit;

namespace Tests.Unit.Backend.Domain.Entities
{
    public class RoleTests
    {
        [Fact]
        public void Constructor_WithValidName_ShouldCreateRole()
        {
            // Arrange
            var name = "Administrator";

            // Act
            var role = new Role(name);

            // Assert
            Assert.Equal(name, role.Name);
            Assert.NotEqual(Guid.Empty, role.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Role(invalidName));
        }

        [Fact]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Role(null!));
        }

        [Fact]
        public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longName = new string('a', 101); // Over the 100 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Role(longName));
        }

        [Fact]
        public void Constructor_WithValidNameAndDescription_ShouldCreateRole()
        {
            // Arrange
            var name = "Administrator";
            var description = "System administrator role";

            // Act
            var role = new Role(name, description);

            // Assert
            Assert.Equal(name, role.Name);
            Assert.Equal(description, role.Description);
        }

        [Fact]
        public void Constructor_WithDescriptionTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var name = "Administrator";
            var longDescription = new string('a', 501); // Over the 500 character limit

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Role(name, longDescription));
        }
    }
}
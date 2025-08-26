using Domain.Entities;
using Xunit;

namespace Tests.Unit.Backend.Domain.Entities
{
    public class PersonTests
    {
        [Fact]
        public void Constructor_WithValidFullName_ShouldCreatePerson()
        {
            // Arrange
            var fullName = "John Doe";

            // Act
            var person = new Person(fullName);

            // Assert
            Assert.Equal(fullName, person.FullName);
            Assert.NotEqual(Guid.Empty, person.Id);
            Assert.NotNull(person.Roles);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidFullName_ShouldThrowArgumentException(string invalidFullName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Person(invalidFullName));
        }

        [Fact]
        public void Constructor_WithNullFullName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Person(null!));
        }

        [Fact]
        public void SetPhone_WithValidPhoneNumber_ShouldSetPhone()
        {
            // Arrange
            var person = new Person("John Doe");
            var phoneNumber = "123-456-7890";

            // Act
            person.Phone = phoneNumber;

            // Assert
            Assert.Equal(phoneNumber, person.Phone);
        }
    }
}
using Domain.ValueObjects;
using Xunit;

namespace Tests.Unit.Backend.Domain.ValueObjects
{
    public class EmailTests
    {
        [Theory]
        [InlineData("user@example.com")]
        [InlineData("test.user@example.co.uk")]
        [InlineData("user+tag@example.com")]
        [InlineData("user_name@example-domain.com")]
        public void Constructor_ShouldCreateEmail_WithValidFormat(string validEmail)
        {
            // Act
            var email = new Email(validEmail);

            // Assert
            Assert.NotNull(email);
            Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_ShouldThrow_WhenEmailIsNullOrEmpty(string invalidEmail)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
            Assert.Contains("Email cannot be empty", ex.Message);
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        [InlineData("user @example.com")]
        [InlineData("user@.com")]
        [InlineData("user@example")]
        public void Constructor_ShouldThrow_WhenEmailFormatIsInvalid(string invalidEmail)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
            Assert.Contains("Email format is invalid", ex.Message);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenEmailIsTooLong()
        {
            // Arrange
            var longEmail = new string('a', 250) + "@example.com";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new Email(longEmail));
            Assert.Contains("Email cannot exceed 256 characters", ex.Message);
        }

        [Fact]
        public void Emails_WithSameValue_ShouldBeEqual()
        {
            // Arrange
            var email1 = new Email("user@example.com");
            var email2 = new Email("USER@EXAMPLE.COM");

            // Act & Assert
            Assert.Equal(email2, email1);
            Assert.Equal(email2.GetHashCode(), email1.GetHashCode());
            Assert.True(email1 == email2);
        }

        [Fact]
        public void Emails_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var email1 = new Email("user1@example.com");
            var email2 = new Email("user2@example.com");

            // Act & Assert
            Assert.NotEqual(email2, email1);
            Assert.NotEqual(email2.GetHashCode(), email1.GetHashCode());
            Assert.True(email1 != email2);
        }

        [Fact]
        public void ToString_ShouldReturnEmailValue()
        {
            // Arrange
            var email = new Email("user@example.com");

            // Act
            var result = email.ToString();

            // Assert
            Assert.Equal("user@example.com", result);
        }

        [Fact]
        public void ImplicitConversion_FromEmail_ShouldReturnValue()
        {
            // Arrange
            var email = new Email("user@example.com");

            // Act
            string value = email;

            // Assert
            Assert.Equal("user@example.com", value);
        }

        [Fact]
        public void ImplicitConversion_FromString_ShouldCreateEmail()
        {
            // Arrange
            string value = "user@example.com";

            // Act
            Email email = value;

            // Assert
            Assert.Equal("user@example.com", email.Value);
        }
    }
}

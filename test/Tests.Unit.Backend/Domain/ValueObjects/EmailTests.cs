using Domain.ValueObjects;
using FluentAssertions;
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
            email.Should().NotBeNull();
            email.Value.Should().Be(validEmail.ToLowerInvariant());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_ShouldThrow_WhenEmailIsNullOrEmpty(string invalidEmail)
        {
            // Act
            var action = () => new Email(invalidEmail);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Email cannot be empty*");
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
            // Act
            var action = () => new Email(invalidEmail);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Email format is invalid*");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenEmailIsTooLong()
        {
            // Arrange
            var longEmail = new string('a', 250) + "@example.com";

            // Act
            var action = () => new Email(longEmail);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Email cannot exceed 256 characters*");
        }

        [Fact]
        public void Emails_WithSameValue_ShouldBeEqual()
        {
            // Arrange
            var email1 = new Email("user@example.com");
            var email2 = new Email("USER@EXAMPLE.COM");

            // Act & Assert
            email1.Should().Be(email2);
            email1.GetHashCode().Should().Be(email2.GetHashCode());
            (email1 == email2).Should().BeTrue();
        }

        [Fact]
        public void Emails_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var email1 = new Email("user1@example.com");
            var email2 = new Email("user2@example.com");

            // Act & Assert
            email1.Should().NotBe(email2);
            email1.GetHashCode().Should().NotBe(email2.GetHashCode());
            (email1 != email2).Should().BeTrue();
        }

        [Fact]
        public void ToString_ShouldReturnEmailValue()
        {
            // Arrange
            var email = new Email("user@example.com");

            // Act
            var result = email.ToString();

            // Assert
            result.Should().Be("user@example.com");
        }

        [Fact]
        public void ImplicitConversion_FromEmail_ShouldReturnValue()
        {
            // Arrange
            var email = new Email("user@example.com");

            // Act
            string value = email;

            // Assert
            value.Should().Be("user@example.com");
        }

        [Fact]
        public void ImplicitConversion_FromString_ShouldCreateEmail()
        {
            // Arrange
            string value = "user@example.com";

            // Act
            Email email = value;

            // Assert
            email.Value.Should().Be("user@example.com");
        }
    }
}
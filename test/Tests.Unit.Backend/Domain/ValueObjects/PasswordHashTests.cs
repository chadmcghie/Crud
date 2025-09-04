using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Backend.Domain.ValueObjects
{
    public class PasswordHashTests
    {
        [Fact]
        public void Constructor_ShouldCreatePasswordHash_WithValidHash()
        {
            // Arrange
            var hashValue = "$2a$10$N9qo8uLOickgx2ZMRZoMye";

            // Act
            var passwordHash = new PasswordHash(hashValue);

            // Assert
            passwordHash.Should().NotBeNull();
            passwordHash.Value.Should().Be(hashValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_ShouldThrow_WhenHashIsNullOrEmpty(string invalidHash)
        {
            // Act
            var action = () => new PasswordHash(invalidHash);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Password hash cannot be empty*");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenHashIsTooShort()
        {
            // Arrange
            var shortHash = "tooshort";

            // Act
            var action = () => new PasswordHash(shortHash);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Password hash format is invalid*");
        }

        [Fact]
        public void PasswordHashes_WithSameValue_ShouldBeEqual()
        {
            // Arrange
            var hash = "$2a$10$N9qo8uLOickgx2ZMRZoMye";
            var passwordHash1 = new PasswordHash(hash);
            var passwordHash2 = new PasswordHash(hash);

            // Act & Assert
            passwordHash1.Should().Be(passwordHash2);
            passwordHash1.GetHashCode().Should().Be(passwordHash2.GetHashCode());
            (passwordHash1 == passwordHash2).Should().BeTrue();
        }

        [Fact]
        public void PasswordHashes_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var passwordHash1 = new PasswordHash("$2a$10$N9qo8uLOickgx2ZMRZoMye");
            var passwordHash2 = new PasswordHash("$2a$10$differenthashvalue123");

            // Act & Assert
            passwordHash1.Should().NotBe(passwordHash2);
            passwordHash1.GetHashCode().Should().NotBe(passwordHash2.GetHashCode());
            (passwordHash1 != passwordHash2).Should().BeTrue();
        }

        [Fact]
        public void ToString_ShouldReturnMaskedValue()
        {
            // Arrange
            var passwordHash = new PasswordHash("$2a$10$N9qo8uLOickgx2ZMRZoMye");

            // Act
            var result = passwordHash.ToString();

            // Assert
            result.Should().Be("***");
        }

        [Fact]
        public void ImplicitConversion_FromPasswordHash_ShouldReturnValue()
        {
            // Arrange
            var hash = "$2a$10$N9qo8uLOickgx2ZMRZoMye";
            var passwordHash = new PasswordHash(hash);

            // Act
            string value = passwordHash;

            // Assert
            value.Should().Be(hash);
        }

        [Fact]
        public void ImplicitConversion_FromString_ShouldCreatePasswordHash()
        {
            // Arrange
            string hash = "$2a$10$N9qo8uLOickgx2ZMRZoMye";

            // Act
            PasswordHash passwordHash = hash;

            // Assert
            passwordHash.Value.Should().Be(hash);
        }
    }
}

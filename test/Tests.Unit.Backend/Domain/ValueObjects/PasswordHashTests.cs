using Domain.ValueObjects;
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
            Assert.NotNull(passwordHash);
            Assert.Equal(hashValue, passwordHash.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_ShouldThrow_WhenHashIsNullOrEmpty(string invalidHash)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new PasswordHash(invalidHash));
            Assert.Contains("Password hash cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenHashIsTooShort()
        {
            // Arrange
            var shortHash = "tooshort";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new PasswordHash(shortHash));
            Assert.Contains("Password hash format is invalid", ex.Message);
        }

        [Fact]
        public void PasswordHashes_WithSameValue_ShouldBeEqual()
        {
            // Arrange
            var hash = "$2a$10$N9qo8uLOickgx2ZMRZoMye";
            var passwordHash1 = new PasswordHash(hash);
            var passwordHash2 = new PasswordHash(hash);

            // Act & Assert
            Assert.Equal(passwordHash2, passwordHash1);
            Assert.Equal(passwordHash2.GetHashCode(), passwordHash1.GetHashCode());
            Assert.True(passwordHash1 == passwordHash2);
        }

        [Fact]
        public void PasswordHashes_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var passwordHash1 = new PasswordHash("$2a$10$N9qo8uLOickgx2ZMRZoMye");
            var passwordHash2 = new PasswordHash("$2a$10$differenthashvalue123");

            // Act & Assert
            Assert.NotEqual(passwordHash2, passwordHash1);
            Assert.NotEqual(passwordHash2.GetHashCode(), passwordHash1.GetHashCode());
            Assert.True(passwordHash1 != passwordHash2);
        }

        [Fact]
        public void ToString_ShouldReturnMaskedValue()
        {
            // Arrange
            var passwordHash = new PasswordHash("$2a$10$N9qo8uLOickgx2ZMRZoMye");

            // Act
            var result = passwordHash.ToString();

            // Assert
            Assert.Equal("***", result);
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
            Assert.Equal(hash, value);
        }

        [Fact]
        public void ImplicitConversion_FromString_ShouldCreatePasswordHash()
        {
            // Arrange
            string hash = "$2a$10$N9qo8uLOickgx2ZMRZoMye";

            // Act
            PasswordHash passwordHash = hash;

            // Assert
            Assert.Equal(hash, passwordHash.Value);
        }
    }
}

using Domain.Entities.Authentication;
using Xunit;

namespace Tests.Unit.Backend.Domain
{
    public class RefreshTokenTests
    {
        [Fact]
        public void Constructor_ShouldCreateRefreshToken_WithValidParameters()
        {
            // Arrange
            var token = "refreshToken123";
            var userId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var refreshToken = new RefreshToken(token, expiresAt, userId);

            // Assert
            Assert.NotNull(refreshToken);
            Assert.NotEqual(Guid.Empty, refreshToken.Id);
            Assert.Equal(token, refreshToken.Token);
            Assert.Equal(userId, refreshToken.UserId);
            Assert.Equal(expiresAt, refreshToken.ExpiresAt);
            Assert.True((DateTime.UtcNow - refreshToken.CreatedAt).TotalSeconds < 1);
            Assert.Null(refreshToken.RevokedAt);
            Assert.True(refreshToken.IsActive);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_ShouldThrow_WhenTokenIsNullOrEmpty(string invalidToken)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new RefreshToken(invalidToken, expiresAt, userId));
            Assert.Contains("Token cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenUserIdIsEmpty()
        {
            // Arrange
            var token = "refreshToken123";
            var userId = Guid.Empty;
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new RefreshToken(token, expiresAt, userId));
            Assert.Contains("UserId cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenExpiresAtIsInPast()
        {
            // Arrange
            var token = "refreshToken123";
            var userId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(-1);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new RefreshToken(token, expiresAt, userId));
            Assert.Contains("Expiration date must be in the future", ex.Message);
        }

        [Fact]
        public void IsActive_ShouldReturnTrue_WhenTokenIsNotRevokedAndNotExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid()
            );

            // Act & Assert
            Assert.True(refreshToken.IsActive);
        }

        [Fact]
        public void IsActive_ShouldReturnFalse_WhenTokenIsRevoked()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid()
            );
            refreshToken.Revoke();

            // Act & Assert
            Assert.False(refreshToken.IsActive);
        }

        [Fact]
        public void IsActive_ShouldReturnFalse_WhenTokenIsExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddSeconds(1),
                Guid.NewGuid()
            );
            System.Threading.Thread.Sleep(1500); // Wait for expiration

            // Act & Assert
            Assert.False(refreshToken.IsActive);
        }

        [Fact]
        public void IsExpired_ShouldReturnTrue_WhenTokenIsExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddSeconds(1),
                Guid.NewGuid()
            );
            System.Threading.Thread.Sleep(1500); // Wait for expiration

            // Act & Assert
            Assert.True(refreshToken.IsExpired);
        }

        [Fact]
        public void IsExpired_ShouldReturnFalse_WhenTokenIsNotExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid()
            );

            // Act & Assert
            Assert.False(refreshToken.IsExpired);
        }

        [Fact]
        public void Revoke_ShouldSetRevokedAt()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid()
            );

            // Act
            refreshToken.Revoke();

            // Assert
            Assert.NotNull(refreshToken.RevokedAt);
            Assert.True((DateTime.UtcNow - refreshToken.RevokedAt.Value).TotalSeconds < 1);
            Assert.False(refreshToken.IsActive);
        }

        [Fact]
        public void Revoke_ShouldNotChangeRevokedAt_WhenAlreadyRevoked()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid()
            );
            refreshToken.Revoke();
            var firstRevokedAt = refreshToken.RevokedAt;
            System.Threading.Thread.Sleep(100);

            // Act
            refreshToken.Revoke();

            // Assert
            Assert.Equal(firstRevokedAt, refreshToken.RevokedAt);
        }

        [Fact]
        public void Equals_ShouldReturnTrue_ForSameToken()
        {
            // Arrange
            var token = "token123";
            var userId = Guid.NewGuid();
            var refreshToken1 = new RefreshToken(token, DateTime.UtcNow.AddDays(7), userId);
            var refreshToken2 = new RefreshToken(token, DateTime.UtcNow.AddDays(7), userId);

            // Act & Assert
            Assert.Equal(refreshToken2.Token, refreshToken1.Token);
        }

        [Fact]
        public void Equals_ShouldReturnFalse_ForDifferentTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken1 = new RefreshToken("token123", DateTime.UtcNow.AddDays(7), userId);
            var refreshToken2 = new RefreshToken("token456", DateTime.UtcNow.AddDays(7), userId);

            // Act & Assert
            Assert.NotEqual(refreshToken2.Token, refreshToken1.Token);
        }
    }
}

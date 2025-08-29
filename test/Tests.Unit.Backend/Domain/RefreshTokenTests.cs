using Domain.Entities.Authentication;
using FluentAssertions;
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
            var refreshToken = new RefreshToken(token, userId, expiresAt);

            // Assert
            refreshToken.Should().NotBeNull();
            refreshToken.Id.Should().NotBeEmpty();
            refreshToken.Token.Should().Be(token);
            refreshToken.UserId.Should().Be(userId);
            refreshToken.ExpiresAt.Should().Be(expiresAt);
            refreshToken.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            refreshToken.RevokedAt.Should().BeNull();
            refreshToken.IsActive.Should().BeTrue();
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

            // Act
            var action = () => new RefreshToken(invalidToken, userId, expiresAt);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Token cannot be empty*");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenUserIdIsEmpty()
        {
            // Arrange
            var token = "refreshToken123";
            var userId = Guid.Empty;
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var action = () => new RefreshToken(token, userId, expiresAt);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("UserId cannot be empty*");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenExpiresAtIsInPast()
        {
            // Arrange
            var token = "refreshToken123";
            var userId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(-1);

            // Act
            var action = () => new RefreshToken(token, userId, expiresAt);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Expiration date must be in the future*");
        }

        [Fact]
        public void IsActive_ShouldReturnTrue_WhenTokenIsNotRevokedAndNotExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(7)
            );

            // Act & Assert
            refreshToken.IsActive.Should().BeTrue();
        }

        [Fact]
        public void IsActive_ShouldReturnFalse_WhenTokenIsRevoked()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(7)
            );
            refreshToken.Revoke();

            // Act & Assert
            refreshToken.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_ShouldReturnFalse_WhenTokenIsExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddSeconds(1)
            );
            System.Threading.Thread.Sleep(1500); // Wait for expiration

            // Act & Assert
            refreshToken.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsExpired_ShouldReturnTrue_WhenTokenIsExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddSeconds(1)
            );
            System.Threading.Thread.Sleep(1500); // Wait for expiration

            // Act & Assert
            refreshToken.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void IsExpired_ShouldReturnFalse_WhenTokenIsNotExpired()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(7)
            );

            // Act & Assert
            refreshToken.IsExpired.Should().BeFalse();
        }

        [Fact]
        public void Revoke_ShouldSetRevokedAt()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(7)
            );

            // Act
            refreshToken.Revoke();

            // Assert
            refreshToken.RevokedAt.Should().NotBeNull();
            refreshToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            refreshToken.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Revoke_ShouldNotChangeRevokedAt_WhenAlreadyRevoked()
        {
            // Arrange
            var refreshToken = new RefreshToken(
                "token123",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(7)
            );
            refreshToken.Revoke();
            var firstRevokedAt = refreshToken.RevokedAt;
            System.Threading.Thread.Sleep(100);

            // Act
            refreshToken.Revoke();

            // Assert
            refreshToken.RevokedAt.Should().Be(firstRevokedAt);
        }

        [Fact]
        public void Equals_ShouldReturnTrue_ForSameToken()
        {
            // Arrange
            var token = "token123";
            var userId = Guid.NewGuid();
            var refreshToken1 = new RefreshToken(token, userId, DateTime.UtcNow.AddDays(7));
            var refreshToken2 = new RefreshToken(token, userId, DateTime.UtcNow.AddDays(7));

            // Act & Assert
            refreshToken1.Token.Should().Be(refreshToken2.Token);
        }

        [Fact]
        public void Equals_ShouldReturnFalse_ForDifferentTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken1 = new RefreshToken("token123", userId, DateTime.UtcNow.AddDays(7));
            var refreshToken2 = new RefreshToken("token456", userId, DateTime.UtcNow.AddDays(7));

            // Act & Assert
            refreshToken1.Token.Should().NotBe(refreshToken2.Token);
        }
    }
}
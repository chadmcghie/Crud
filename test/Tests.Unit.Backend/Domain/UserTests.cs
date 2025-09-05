using Domain.Entities.Authentication;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Backend.Domain
{
    public class UserTests
    {
        [Fact]
        public void Constructor_ShouldCreateUser_WithValidEmailAndPasswordHash()
        {
            // Arrange
            var email = new Email("user@example.com");
            var passwordHash = new PasswordHash("$2a$10$hashedpassword123");

            // Act
            var user = new User(email, passwordHash);

            // Assert
            user.Should().NotBeNull();
            user.Id.Should().NotBeEmpty();
            user.Email.Should().Be(email);
            user.PasswordHash.Should().Be(passwordHash);
            user.Roles.Should().NotBeNull();
            user.Roles.Should().Contain("User");
            user.RefreshTokens.Should().NotBeNull();
            user.RefreshTokens.Should().BeEmpty();
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AddRefreshToken_ShouldAddTokenToCollection()
        {
            // Arrange
            var user = CreateTestUser();
            var token = "refreshToken123";
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var refreshToken = user.AddRefreshToken(token, expiresAt);

            // Assert
            user.RefreshTokens.Should().HaveCount(1);
            user.RefreshTokens.Should().Contain(refreshToken);
            refreshToken.Token.Should().Be(token);
            refreshToken.UserId.Should().Be(user.Id);
            refreshToken.ExpiresAt.Should().Be(expiresAt);
            refreshToken.IsActive.Should().BeTrue();
        }

        [Fact]
        public void RevokeRefreshToken_ShouldMarkTokenAsRevoked()
        {
            // Arrange
            var user = CreateTestUser();
            var token = "refreshToken123";
            var refreshToken = user.AddRefreshToken(token, DateTime.UtcNow.AddDays(7));

            // Act
            var result = user.RevokeRefreshToken(token);

            // Assert
            result.Should().BeTrue();
            refreshToken.IsActive.Should().BeFalse();
            refreshToken.RevokedAt.Should().NotBeNull();
            refreshToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RevokeRefreshToken_ShouldReturnFalse_WhenTokenNotFound()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            var result = user.RevokeRefreshToken("nonexistentToken");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetActiveRefreshToken_ShouldReturnActiveToken()
        {
            // Arrange
            var user = CreateTestUser();
            var activeToken = "activeToken";
            var expiredToken = "expiredToken";
            var revokedToken = "revokedToken";

            user.AddRefreshToken(activeToken, DateTime.UtcNow.AddDays(7));

            // Add expired token using test helper
            var expiredRefreshToken = RefreshToken.CreateForTesting(expiredToken, user.Id, DateTime.UtcNow.AddDays(-1));
            user.AddRefreshTokenForTesting(expiredRefreshToken);

            user.AddRefreshToken(revokedToken, DateTime.UtcNow.AddDays(7));
            user.RevokeRefreshToken(revokedToken);

            // Act
            var result = user.GetActiveRefreshToken(activeToken);

            // Assert
            result.Should().NotBeNull();
            result!.Token.Should().Be(activeToken);
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public void GetActiveRefreshToken_ShouldReturnNull_WhenTokenExpired()
        {
            // Arrange
            var user = CreateTestUser();
            var expiredToken = "expiredToken";

            // Add expired token using test helper
            var expiredRefreshToken = RefreshToken.CreateForTesting(expiredToken, user.Id, DateTime.UtcNow.AddDays(-1));
            user.AddRefreshTokenForTesting(expiredRefreshToken);

            // Act
            var result = user.GetActiveRefreshToken(expiredToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void AddRole_ShouldAddRoleToUser()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            user.AddRole("Admin");

            // Assert
            user.Roles.Should().Contain("Admin");
            user.Roles.Should().Contain("User");
            user.Roles.Should().HaveCount(2);
        }

        [Fact]
        public void AddRole_ShouldNotDuplicateRole()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            user.AddRole("User");

            // Assert
            user.Roles.Should().Contain("User");
            user.Roles.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveRole_ShouldRemoveRoleFromUser()
        {
            // Arrange
            var user = CreateTestUser();
            user.AddRole("Admin");

            // Act
            user.RemoveRole("Admin");

            // Assert
            user.Roles.Should().NotContain("Admin");
            user.Roles.Should().Contain("User");
        }

        [Fact]
        public void UpdatePassword_ShouldUpdatePasswordHash()
        {
            // Arrange
            var user = CreateTestUser();
            var newPasswordHash = new PasswordHash("$2a$10$newhashedpassword");
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            System.Threading.Thread.Sleep(10); // Ensure time difference
            user.UpdatePassword(newPasswordHash);

            // Assert
            user.PasswordHash.Should().Be(newPasswordHash);
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void CleanupExpiredTokens_ShouldRemoveExpiredTokens()
        {
            // Arrange
            var user = CreateTestUser();
            user.AddRefreshToken("activeToken", DateTime.UtcNow.AddDays(7));

            // Add expired tokens using test helper
            var expiredToken1 = RefreshToken.CreateForTesting("expiredToken1", user.Id, DateTime.UtcNow.AddDays(-1));
            var expiredToken2 = RefreshToken.CreateForTesting("expiredToken2", user.Id, DateTime.UtcNow.AddDays(-2));
            user.AddRefreshTokenForTesting(expiredToken1);
            user.AddRefreshTokenForTesting(expiredToken2);

            // Act
            var removedCount = user.CleanupExpiredTokens();

            // Assert
            removedCount.Should().Be(2);
            user.RefreshTokens.Should().HaveCount(1);
            user.RefreshTokens.First().Token.Should().Be("activeToken");
        }

        private User CreateTestUser()
        {
            return new User(
                new Email("test@example.com"),
                new PasswordHash("$2a$10$hashedpassword")
            );
        }
    }
}

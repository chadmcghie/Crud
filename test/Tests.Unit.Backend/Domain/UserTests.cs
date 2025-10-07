using Domain.Entities.Authentication;
using Domain.ValueObjects;
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
            Assert.NotNull(user);
            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.Equal(email, user.Email);
            Assert.Equal(passwordHash, user.PasswordHash);
            Assert.NotNull(user.Roles);
            Assert.Contains("User", user.Roles);
            Assert.NotNull(user.RefreshTokens);
            Assert.Empty(user.RefreshTokens);
            Assert.True((DateTime.UtcNow - user.CreatedAt).TotalSeconds < 1);
            Assert.True((DateTime.UtcNow - user.UpdatedAt).TotalSeconds < 1);
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
            Assert.Single(user.RefreshTokens);
            Assert.Contains(refreshToken, user.RefreshTokens);
            Assert.Equal(token, refreshToken.Token);
            Assert.Equal(user.Id, refreshToken.UserId);
            Assert.Equal(expiresAt, refreshToken.ExpiresAt);
            Assert.True(refreshToken.IsActive);
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
            Assert.True(result);
            Assert.False(refreshToken.IsActive);
            Assert.NotNull(refreshToken.RevokedAt);
            Assert.True((DateTime.UtcNow - refreshToken.RevokedAt.Value).TotalSeconds < 1);
        }

        [Fact]
        public void RevokeRefreshToken_ShouldReturnFalse_WhenTokenNotFound()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            var result = user.RevokeRefreshToken("nonexistentToken");

            // Assert
            Assert.False(result);
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
            Assert.NotNull(result);
            Assert.Equal(activeToken, result!.Token);
            Assert.True(result.IsActive);
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
            Assert.Null(result);
        }

        [Fact]
        public void AddRole_ShouldAddRoleToUser()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            user.AddRole("Admin");

            // Assert
            Assert.Contains("Admin", user.Roles);
            Assert.Contains("User", user.Roles);
            Assert.Equal(2, user.Roles.Count);
        }

        [Fact]
        public void AddRole_ShouldNotDuplicateRole()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            user.AddRole("User");

            // Assert
            Assert.Contains("User", user.Roles);
            Assert.Single(user.Roles);
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
            Assert.DoesNotContain("Admin", user.Roles);
            Assert.Contains("User", user.Roles);
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
            Assert.Equal(newPasswordHash, user.PasswordHash);
            Assert.True(user.UpdatedAt > originalUpdatedAt);
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
            Assert.Equal(2, removedCount);
            Assert.Single(user.RefreshTokens);
            Assert.Equal("activeToken", user.RefreshTokens.First().Token);
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

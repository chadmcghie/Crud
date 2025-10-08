using Domain.Entities.Authentication;
using Xunit;

namespace Tests.Unit.Backend.Domain;

public class PasswordResetTokenTests
{
    [Fact]
    public void Create_ShouldCreatePasswordResetToken_WithValidParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var token = PasswordResetToken.Create(userId, expiresAt);

        // Assert
        Assert.NotNull(token);
        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.NotNull(token.Token);
        Assert.NotEmpty(token.Token);
        Assert.True(token.Token.Length >= 32); // Minimum secure token length
        Assert.Equal(userId, token.UserId);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.False(token.IsUsed);
        Assert.Null(token.UsedAt);
        Assert.True((DateTime.UtcNow - token.CreatedAt).TotalSeconds < 1);
        Assert.False(token.IsExpired);
        Assert.True(token.IsValid);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var token1 = PasswordResetToken.Create(userId, expiresAt);
        var token2 = PasswordResetToken.Create(userId, expiresAt);

        // Assert
        Assert.NotEqual(token2.Token, token1.Token);
    }

    [Fact]
    public void Create_WithDefaultExpiration_ShouldSetOneHourExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = PasswordResetToken.Create(userId);

        // Assert
        Assert.True((token.ExpiresAt - DateTime.UtcNow.AddHours(1)).TotalSeconds < 1);
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        // Arrange
        var userId = Guid.Empty;
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => PasswordResetToken.Create(userId, expiresAt));
        Assert.Contains("UserId cannot be empty", ex.Message);
    }

    [Fact]
    public void Create_ShouldThrow_WhenExpiresAtIsInPast()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => PasswordResetToken.Create(userId, expiresAt));
        Assert.Contains("Expiration date must be in the future", ex.Message);
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenTokenIsExpired()
    {
        // Arrange
        var token = PasswordResetToken.CreateForTesting(
            Guid.NewGuid(),
            "test-token",
            DateTime.UtcNow.AddMinutes(-1) // Past expiration
        );

        // Act & Assert
        Assert.True(token.IsExpired);
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenTokenIsNotExpired()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act & Assert
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenTokenIsNotExpiredAndNotUsed()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act & Assert
        Assert.True(token.IsValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenTokenIsUsed()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        token.MarkAsUsed();

        // Act & Assert
        Assert.False(token.IsValid);
        Assert.True(token.IsUsed);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenTokenIsExpired()
    {
        // Arrange
        var token = PasswordResetToken.CreateForTesting(
            Guid.NewGuid(),
            "test-token",
            DateTime.UtcNow.AddMinutes(-1)
        );

        // Act & Assert
        Assert.False(token.IsValid);
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedFlagAndTimestamp()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        token.MarkAsUsed();

        // Assert
        Assert.True(token.IsUsed);
        Assert.NotNull(token.UsedAt);
        Assert.True((DateTime.UtcNow - token.UsedAt.Value).TotalSeconds < 1);
        Assert.False(token.IsValid);
    }

    [Fact]
    public void MarkAsUsed_ShouldNotChangeUsedAt_WhenAlreadyUsed()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        token.MarkAsUsed();
        var firstUsedAt = token.UsedAt;
        System.Threading.Thread.Sleep(100);

        // Act
        token.MarkAsUsed();

        // Assert
        Assert.Equal(firstUsedAt, token.UsedAt);
    }

    [Fact]
    public void Expire_ShouldForceTokenExpiration()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        token.Expire();

        // Assert
        Assert.True(token.IsExpired);
        Assert.False(token.IsValid);
        Assert.True(token.ExpiresAt < DateTime.UtcNow);
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_ForMatchingValidToken()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        var tokenString = token.Token;

        // Act
        var isValid = token.ValidateToken(tokenString);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForNonMatchingToken()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        var isValid = token.ValidateToken("different-token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForExpiredToken()
    {
        // Arrange
        var token = PasswordResetToken.CreateForTesting(
            Guid.NewGuid(),
            "test-token",
            DateTime.UtcNow.AddMinutes(-1)
        );

        // Act
        var isValid = token.ValidateToken("test-token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForUsedToken()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        var tokenString = token.Token;
        token.MarkAsUsed();

        // Act
        var isValid = token.ValidateToken(tokenString);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateToken_ShouldReturnFalse_ForNullOrEmptyToken(string invalidToken)
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        var isValid = token.ValidateToken(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldUseConstantTimeComparison()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        var tokenString = token.Token;

        // Act - Measure time for matching token
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            token.ValidateToken(tokenString);
        }
        sw1.Stop();

        // Act - Measure time for non-matching token of same length
        var differentToken = new string('X', tokenString.Length);
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            token.ValidateToken(differentToken);
        }
        sw2.Stop();

        // Assert - Times should be similar (within reasonable margin)
        // This is a basic timing attack protection test
        var timeDifference = Math.Abs(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds);
        Assert.True(timeDifference < 50); // Reasonable threshold for constant-time comparison
    }
}

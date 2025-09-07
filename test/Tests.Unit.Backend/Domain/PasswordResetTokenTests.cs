using Domain.Entities.Authentication;
using FluentAssertions;
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
        token.Should().NotBeNull();
        token.Id.Should().NotBeEmpty();
        token.Token.Should().NotBeNullOrEmpty();
        token.Token.Length.Should().BeGreaterOrEqualTo(32); // Minimum secure token length
        token.UserId.Should().Be(userId);
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsUsed.Should().BeFalse();
        token.UsedAt.Should().BeNull();
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        token.IsExpired.Should().BeFalse();
        token.IsValid.Should().BeTrue();
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
        token1.Token.Should().NotBe(token2.Token);
    }

    [Fact]
    public void Create_WithDefaultExpiration_ShouldSetOneHourExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = PasswordResetToken.Create(userId);

        // Assert
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        // Arrange
        var userId = Guid.Empty;
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var action = () => PasswordResetToken.Create(userId, expiresAt);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("UserId cannot be empty*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenExpiresAtIsInPast()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var action = () => PasswordResetToken.Create(userId, expiresAt);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Expiration date must be in the future*");
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
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenTokenIsNotExpired()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenTokenIsNotExpiredAndNotUsed()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act & Assert
        token.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenTokenIsUsed()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));
        token.MarkAsUsed();

        // Act & Assert
        token.IsValid.Should().BeFalse();
        token.IsUsed.Should().BeTrue();
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
        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedFlagAndTimestamp()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        token.MarkAsUsed();

        // Assert
        token.IsUsed.Should().BeTrue();
        token.UsedAt.Should().NotBeNull();
        token.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        token.IsValid.Should().BeFalse();
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
        token.UsedAt.Should().Be(firstUsedAt);
    }

    [Fact]
    public void Expire_ShouldForceTokenExpiration()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        token.Expire();

        // Assert
        token.IsExpired.Should().BeTrue();
        token.IsValid.Should().BeFalse();
        token.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
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
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForNonMatchingToken()
    {
        // Arrange
        var token = PasswordResetToken.Create(Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

        // Act
        var isValid = token.ValidateToken("different-token");

        // Assert
        isValid.Should().BeFalse();
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
        isValid.Should().BeFalse();
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
        isValid.Should().BeFalse();
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
        isValid.Should().BeFalse();
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
        timeDifference.Should().BeLessThan(50); // Reasonable threshold for constant-time comparison
    }
}

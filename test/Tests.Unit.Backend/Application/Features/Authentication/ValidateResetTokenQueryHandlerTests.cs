using System;
using System.Threading;
using System.Threading.Tasks;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.Application.Features.Authentication;

public class ValidateResetTokenQueryHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _mockTokenRepository;
    private readonly Mock<ILogger<ValidateResetTokenQueryHandler>> _mockLogger;
    private readonly ValidateResetTokenQueryHandler _handler;

    public ValidateResetTokenQueryHandlerTests()
    {
        _mockTokenRepository = new Mock<IPasswordResetTokenRepository>();
        _mockLogger = new Mock<ILogger<ValidateResetTokenQueryHandler>>();

        _handler = new ValidateResetTokenQueryHandler(
            _mockTokenRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnValidResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        var query = new ValidateResetTokenQuery { Token = token.Token };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.False(result.IsUsed);
        Assert.True(Math.Abs((result.ExpiresAt!.Value - DateTime.UtcNow.AddHours(1)).TotalSeconds) < 5);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldReturnExpiredResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.CreateForTesting(userId, "test-token", DateTime.UtcNow.AddHours(-1));
        var query = new ValidateResetTokenQuery { Token = token.Token };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.IsExpired);
        Assert.False(result.IsUsed);
        Assert.Equal(token.ExpiresAt, result.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WithUsedToken_ShouldReturnUsedResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        token.MarkAsUsed();
        var query = new ValidateResetTokenQuery { Token = token.Token };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.True(result.IsUsed);
        Assert.True(Math.Abs((result.ExpiresAt!.Value - DateTime.UtcNow.AddHours(1)).TotalSeconds) < 5);
    }

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var query = new ValidateResetTokenQuery { Token = "non-existent-token" };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.False(result.IsUsed);
        Assert.Null(result.ExpiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WithEmptyToken_ShouldReturnInvalidResponse(string? invalidToken)
    {
        // Arrange
        var query = new ValidateResetTokenQuery { Token = invalidToken! };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.False(result.IsUsed);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WithMalformedToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var query = new ValidateResetTokenQuery { Token = "not-a-base64-token!" };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.False(result.IsUsed);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public async Task Handle_ShouldLogTokenValidation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        var query = new ValidateResetTokenQuery { Token = token.Token };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token validation requested")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

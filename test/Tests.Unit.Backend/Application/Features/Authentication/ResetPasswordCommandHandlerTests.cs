using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Abstractions;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.Application.Features.Authentication;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _mockTokenRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ILogger<ResetPasswordCommandHandler>> _mockLogger;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _mockTokenRepository = new Mock<IPasswordResetTokenRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockLogger = new Mock<ILogger<ResetPasswordCommandHandler>>();

        _handler = new ResetPasswordCommandHandler(
            _mockTokenRepository.Object,
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldResetPasswordSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        var command = new ResetPasswordCommand
        {
            Token = token.Token,
            NewPassword = "NewP@ssw0rd123!"
        };
        var user = CreateTestUser(userId);

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("$2a$11$K5Y7Tc5kZPCK7CJnBpRCY.Rk7UqFp7YZ3pEZWx9xGKOq6Y/2YxLha");

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTokenRepository
            .Setup(x => x.UpdateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTokenRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUserRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.Equal("Your password has been reset successfully.", result.Message);

        _mockPasswordHasher.Verify(x => x.HashPassword(command.NewPassword), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTokenRepository.Verify(x => x.UpdateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.CreateForTesting(userId, "test-token", DateTime.UtcNow.AddHours(-1));
        var command = new ResetPasswordCommand
        {
            Token = token.Token,
            NewPassword = "NewP@ssw0rd123!"
        };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("The password reset link has expired. Please request a new one.", result.Error);
    }

    [Fact]
    public async Task Handle_WithUsedToken_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        token.MarkAsUsed();

        var command = new ResetPasswordCommand
        {
            Token = token.Token,
            NewPassword = "NewP@ssw0rd123!"
        };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("This password reset link has already been used.", result.Error);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Token = "invalid-token",
            NewPassword = "NewP@ssw0rd123!"
        };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid or expired password reset link.", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WithEmptyToken_ShouldReturnError(string? invalidToken)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Token = invalidToken!,
            NewPassword = "NewP@ssw0rd123!"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Token is required", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WithEmptyPassword_ShouldReturnError(string? invalidPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Token = "valid-token",
            NewPassword = invalidPassword!
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Password is required", result.Error);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nouppercase123!")]
    [InlineData("NOLOWERCASE123!")]
    [InlineData("NoNumbers!")]
    [InlineData("NoSpecialChar123")]
    public async Task Handle_WithWeakPassword_ShouldReturnError(string weakPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Token = "valid-token",
            NewPassword = weakPassword
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password must", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        var command = new ResetPasswordCommand
        {
            Token = token.Token,
            NewPassword = "NewP@ssw0rd123!"
        };

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User account not found.", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldLogSecurityEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Create(userId);
        var command = new ResetPasswordCommand
        {
            Token = token.Token,
            NewPassword = "NewP@ssw0rd123!"
        };
        var user = CreateTestUser(userId);

        _mockTokenRepository
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("$2a$11$K5Y7Tc5kZPCK7CJnBpRCY.Rk7UqFp7YZ3pEZWx9xGKOq6Y/2YxLha");

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTokenRepository
            .Setup(x => x.UpdateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockTokenRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUserRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password reset successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private User CreateTestUser(Guid? id = null)
    {
        return User.CreateForTesting(
            id ?? Guid.NewGuid(),
            new Email("test@example.com"),
            new PasswordHash("$2a$11$K5Y7Tc5kZPCK7CJnBpRCY.Rk7UqFp7YZ3pEZWx9xGKOq6Y/2YxLha"),
            "Test",
            "User"
        );
    }
}

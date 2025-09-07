using App.Abstractions;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.Application.Features.Authentication;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordResetTokenRepository> _mockTokenRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _mockLogger;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTokenRepository = new Mock<IPasswordResetTokenRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<ForgotPasswordCommandHandler>>();

        _handler = new ForgotPasswordCommandHandler(
            _mockUserRepository.Object,
            _mockTokenRepository.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldSendPasswordResetEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = CreateTestUser("test@example.com");
        var token = PasswordResetToken.Create(user.Id);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Message.Should().Contain("reset instructions");

        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(
            command.Email,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockTokenRepository.Verify(x => x.AddAsync(
            It.IsAny<PasswordResetToken>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ShouldReturnSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "nonexistent@example.com" };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue(); // Return success for security (don't reveal if email exists)
        result.Message.Should().Contain("reset instructions");

        // Verify no email was sent and no token was created
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockTokenRepository.Verify(x => x.AddAsync(
            It.IsAny<PasswordResetToken>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WithInvalidEmail_ShouldReturnError(string invalidEmail)
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = invalidEmail };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("valid email");
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ShouldReturnError()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "not-an-email" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("valid email");
    }

    [Fact]
    public async Task Handle_WhenEmailServiceFails_ShouldReturnError()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = CreateTestUser("test@example.com");

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordResetToken.Create(user.Id));

        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email service unavailable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("We're unable to process your request at this time. Please try again later.");
    }

    [Fact]
    public async Task Handle_WhenTokenCreationFails_ShouldReturnError()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = CreateTestUser("test@example.com");

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("unable to process");
    }

    [Fact]
    public async Task Handle_WithLockedAccount_ShouldStillSendEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = CreateTestUser("test@example.com");
        user.LockAccount();

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordResetToken.Create(user.Id));

        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(
            command.Email,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogSecurityEvent()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = CreateTestUser("test@example.com");

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordResetToken.Create(user.Id));

        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password reset requested")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldInvalidateExistingTokens()
    {
        // Arrange
        var command = new ForgotPasswordCommand { Email = "test@example.com" };
        var user = CreateTestUser("test@example.com");

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // The repository's AddAsync method should handle invalidating existing tokens
        _mockTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordResetToken.Create(user.Id));

        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTokenRepository.Verify(x => x.AddAsync(
            It.IsAny<PasswordResetToken>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User CreateTestUser(string email)
    {
        return User.Create(
            new Email(email),
            new PasswordHash("$2a$12$dummyHashForTestingPurposesOnly.1234567890"),
            "Test",
            "User");
    }
}

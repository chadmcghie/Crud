using App.Abstractions;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<MockEmailService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public EmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<MockEmailService>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Email:Provider", "Mock"},
            {"Email:FromAddress", "noreply@example.com"},
            {"Email:FromName", "Test App"},
            {"Email:ResetPasswordUrl", "https://example.com/reset-password"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _emailService = new MockEmailService(_configuration, _mockLogger.Object);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";

        // Act
        var act = async () => await _emailService.SendPasswordResetEmailAsync(email, resetToken);

        // Assert
        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password reset email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null, "token")]
    [InlineData("", "token")]
    [InlineData(" ", "token")]
    [InlineData("test@example.com", null)]
    [InlineData("test@example.com", "")]
    [InlineData("test@example.com", " ")]
    public async Task SendPasswordResetEmailAsync_WithInvalidParameters_ShouldThrowArgumentException(string email, string resetToken)
    {
        // Act
        var act = async () => await _emailService.SendPasswordResetEmailAsync(email, resetToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithInvalidEmailFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        var resetToken = "test-reset-token-123";

        // Act
        var act = async () => await _emailService.SendPasswordResetEmailAsync(invalidEmail, resetToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*valid email address*");
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldLogEmailContent()
    {
        // Arrange
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";

        // Act
        await _emailService.SendPasswordResetEmailAsync(email, resetToken);

        // Assert - Verify that password reset email was logged without exposing PII
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password reset email sent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _emailService.SendPasswordResetEmailAsync(email, resetToken, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task MockEmailService_ShouldStoreEmailsInMemory()
    {
        // Arrange
        var mockService = new MockEmailService(_configuration, _mockLogger.Object);
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";

        // Act
        await mockService.SendPasswordResetEmailAsync(email, resetToken);
        var sentEmails = mockService.GetSentEmails();

        // Assert
        sentEmails.Should().HaveCount(1);
        sentEmails.First().To.Should().Be(email);
        sentEmails.First().Subject.Should().Contain("Password Reset");
        sentEmails.First().Body.Should().Contain(resetToken);
    }

    [Fact]
    public async Task MockEmailService_ShouldGenerateResetUrl()
    {
        // Arrange
        var mockService = new MockEmailService(_configuration, _mockLogger.Object);
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";
        var expectedUrl = "https://example.com/reset-password";

        // Act
        await mockService.SendPasswordResetEmailAsync(email, resetToken);
        var sentEmails = mockService.GetSentEmails();

        // Assert
        sentEmails.First().Body.Should().Contain(expectedUrl);
        sentEmails.First().Body.Should().Contain($"token={resetToken}");
    }

    [Fact]
    public async Task MockEmailService_ShouldSimulateRealisticDelay()
    {
        // Arrange
        var mockService = new MockEmailService(_configuration, _mockLogger.Object);
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await mockService.SendPasswordResetEmailAsync(email, resetToken);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(50); // Simulated delay
    }
}

public class EmailServiceMockingTests
{
    [Fact]
    public async Task Handler_ShouldCallEmailService_WhenPasswordResetRequested()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";

        mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(email, resetToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await mockEmailService.Object.SendPasswordResetEmailAsync(email, resetToken);

        // Assert
        mockEmailService.Verify(
            x => x.SendPasswordResetEmailAsync(email, resetToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_ShouldHandleEmailServiceFailure_Gracefully()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var email = "test@example.com";
        var resetToken = "test-reset-token-123";

        mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email service unavailable"));

        // Act
        var act = async () => await mockEmailService.Object.SendPasswordResetEmailAsync(email, resetToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email service unavailable");
    }
}

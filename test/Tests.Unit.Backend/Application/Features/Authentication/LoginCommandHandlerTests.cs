using App.Abstractions;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tests.Unit.Backend.Application.Features.Authentication;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<LoginCommandHandler>> _mockLogger;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockLogger = new Mock<ILogger<LoginCommandHandler>>();

        _handler = new LoginCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object,
            _mockLogger.Object);
    }

    public class Handle : LoginCommandHandlerTests
    {
        [Fact]
        public async Task WithValidCredentials_ShouldReturnTokens()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#"
            };

            var passwordHash = "hashed_password_with_proper_length_for_bcrypt";
            var user = User.Create(
                new Email(command.Email),
                new PasswordHash(passwordHash),
                "John",
                "Doe");

            var accessToken = "access_token_value";
            var refreshTokenValue = "refresh_token_value";

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.Password,
                passwordHash))
                .Returns(true);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(user))
                .Returns(accessToken);

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshTokenValue);

            _mockUserRepository.Setup(x => x.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AccessToken.Should().Be(accessToken);
            result.RefreshToken.Should().Be(refreshTokenValue);
            result.Email.Should().Be(command.Email);
            result.UserId.Should().Be(user.Id);
            result.Roles.Should().NotBeNull();

            _mockUserRepository.Verify(x => x.UpdateAsync(
                It.Is<User>(u => u.RefreshTokens.Any(rt => rt.Token == refreshTokenValue)),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _mockPasswordHasher.Verify(x => x.VerifyPassword(command.Password, passwordHash), Times.Once);
            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(user), Times.Once);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task WithInvalidEmail_ShouldReturnFailure()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "nonexistent@example.com",
                Password = "Test123!@#"
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Invalid email or password");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockPasswordHasher.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task WithInvalidPassword_ShouldReturnFailure()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var passwordHash = "hashed_password_with_proper_length_for_bcrypt";
            var user = User.Create(
                new Email(command.Email),
                new PasswordHash(passwordHash),
                "John",
                "Doe");

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.Password,
                passwordHash))
                .Returns(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Invalid email or password");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithLockedAccount_ShouldReturnFailure()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#"
            };

            var passwordHash = "hashed_password_with_proper_length_for_bcrypt";
            var user = User.Create(
                new Email(command.Email),
                new PasswordHash(passwordHash),
                "John",
                "Doe");

            user.LockAccount();

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Account is locked");
            result.AccessToken.Should().BeNull();

            _mockPasswordHasher.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));

            _mockUserRepository.Verify(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task WithEmptyEmail_ShouldReturnFailure(string? email)
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = email!,
                Password = "Test123!@#"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Email");

            _mockUserRepository.Verify(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task WithEmptyPassword_ShouldReturnFailure(string? password)
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = password!
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Password");

            _mockUserRepository.Verify(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#"
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));

            exception.Message.Should().Be("Database error");
        }

        [Fact]
        public async Task WithCancellationRequested_ShouldCancelOperation()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#"
            };

            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task ShouldCleanupExpiredRefreshTokens()
        {
            // Arrange
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#"
            };

            var passwordHash = "hashed_password_with_proper_length_for_bcrypt";
            var user = User.Create(
                new Email(command.Email),
                new PasswordHash(passwordHash),
                "John",
                "Doe");

            // Add expired refresh token
            var expiredToken = RefreshToken.CreateForTesting(
                "expired_token",
                user.Id,
                DateTime.UtcNow.AddDays(-1));
            user.AddRefreshToken(expiredToken);

            var accessToken = "access_token_value";
            var refreshTokenValue = "refresh_token_value";

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.Password,
                passwordHash))
                .Returns(true);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(user))
                .Returns(accessToken);

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshTokenValue);

            _mockUserRepository.Setup(x => x.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            _mockUserRepository.Verify(x => x.UpdateAsync(
                It.Is<User>(u =>
                    !u.RefreshTokens.Any(rt => rt.Token == "expired_token") &&
                    u.RefreshTokens.Any(rt => rt.Token == refreshTokenValue)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

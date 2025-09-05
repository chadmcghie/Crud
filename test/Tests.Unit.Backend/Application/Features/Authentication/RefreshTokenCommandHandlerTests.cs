using App.Abstractions;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tests.Unit.Backend.Application.Features.Authentication;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _mockLogger;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockLogger = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(
            _mockUserRepository.Object,
            _mockJwtTokenService.Object,
            _mockLogger.Object);
    }

    public class Handle : RefreshTokenCommandHandlerTests
    {
        [Fact]
        public async Task WithValidRefreshToken_ShouldReturnNewTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new RefreshTokenCommand
            {
                RefreshToken = "valid_refresh_token"
            };

            var user = User.Create(
                new Email("test@example.com"),
                new PasswordHash("hashed_password_with_proper_length_for_bcrypt"),
                "John",
                "Doe");

            var existingRefreshToken = new RefreshToken(
                command.RefreshToken,
                DateTime.UtcNow.AddDays(7),
                user.Id);
            user.AddRefreshToken(existingRefreshToken);

            var newAccessToken = "new_access_token";
            var newRefreshTokenValue = "new_refresh_token";

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(user))
                .Returns(newAccessToken);

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshTokenValue);

            _mockUserRepository.Setup(x => x.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AccessToken.Should().Be(newAccessToken);
            result.RefreshToken.Should().Be(newRefreshTokenValue);
            result.Email.Should().Be(user.Email.Value);
            result.UserId.Should().Be(user.Id);

            _mockUserRepository.Verify(x => x.UpdateAsync(
                It.Is<User>(u =>
                    !u.RefreshTokens.Any(rt => rt.Token == command.RefreshToken && !rt.IsRevoked) &&
                    u.RefreshTokens.Any(rt => rt.Token == newRefreshTokenValue)),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(user), Times.Once);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task WithInvalidRefreshToken_ShouldReturnFailure()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "invalid_refresh_token"
            };

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Invalid refresh token");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithExpiredRefreshToken_ShouldReturnFailure()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "expired_refresh_token"
            };

            var user = User.Create(
                new Email("test@example.com"),
                new PasswordHash("hashed_password_with_proper_length_for_bcrypt"),
                "John",
                "Doe");

            var expiredRefreshToken = RefreshToken.CreateForTesting(
                command.RefreshToken,
                user.Id,
                DateTime.UtcNow.AddDays(-1)); // Expired
            user.AddRefreshToken(expiredRefreshToken);

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Refresh token has expired");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithRevokedRefreshToken_ShouldReturnFailure()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "revoked_refresh_token"
            };

            var user = User.Create(
                new Email("test@example.com"),
                new PasswordHash("hashed_password_with_proper_length_for_bcrypt"),
                "John",
                "Doe");

            var revokedRefreshToken = new RefreshToken(
                command.RefreshToken,
                DateTime.UtcNow.AddDays(7),
                user.Id);
            revokedRefreshToken.Revoke();
            user.AddRefreshToken(revokedRefreshToken);

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Refresh token has been revoked");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithLockedUserAccount_ShouldReturnFailure()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "valid_refresh_token"
            };

            var user = User.Create(
                new Email("test@example.com"),
                new PasswordHash("hashed_password_with_proper_length_for_bcrypt"),
                "John",
                "Doe");

            user.LockAccount();

            var refreshToken = new RefreshToken(
                command.RefreshToken,
                DateTime.UtcNow.AddDays(7),
                user.Id);
            user.AddRefreshToken(refreshToken);

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Account is locked");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));

            _mockUserRepository.Verify(x => x.GetByRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task WithEmptyRefreshToken_ShouldReturnFailure(string? refreshToken)
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = refreshToken!
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Refresh token");

            _mockUserRepository.Verify(x => x.GetByRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "valid_refresh_token"
            };

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
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
            var command = new RefreshTokenCommand
            {
                RefreshToken = "valid_refresh_token"
            };

            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task ShouldRotateRefreshToken()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "old_refresh_token"
            };

            var user = User.Create(
                new Email("test@example.com"),
                new PasswordHash("hashed_password_with_proper_length_for_bcrypt"),
                "John",
                "Doe");

            var oldRefreshToken = new RefreshToken(
                command.RefreshToken,
                DateTime.UtcNow.AddDays(7),
                user.Id);
            user.AddRefreshToken(oldRefreshToken);

            var newAccessToken = "new_access_token";
            var newRefreshTokenValue = "new_refresh_token";

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(user))
                .Returns(newAccessToken);

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshTokenValue);

            _mockUserRepository.Setup(x => x.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RefreshToken.Should().Be(newRefreshTokenValue);
            result.RefreshToken.Should().NotBe(command.RefreshToken);

            _mockUserRepository.Verify(x => x.UpdateAsync(
                It.Is<User>(u =>
                    u.RefreshTokens.Any(rt => rt.Token == command.RefreshToken && rt.IsRevoked) &&
                    u.RefreshTokens.Any(rt => rt.Token == newRefreshTokenValue && !rt.IsRevoked)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldCleanupExpiredTokens()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                RefreshToken = "valid_refresh_token"
            };

            var user = User.Create(
                new Email("test@example.com"),
                new PasswordHash("hashed_password_with_proper_length_for_bcrypt"),
                "John",
                "Doe");

            // Add multiple expired tokens
            for (int i = 0; i < 5; i++)
            {
                var expiredToken = RefreshToken.CreateForTesting(
                    $"expired_token_{i}",
                    user.Id,
                    DateTime.UtcNow.AddDays(-i - 1));
                user.AddRefreshToken(expiredToken);
            }

            var validToken = new RefreshToken(
                command.RefreshToken,
                DateTime.UtcNow.AddDays(7),
                user.Id);
            user.AddRefreshToken(validToken);

            var newAccessToken = "new_access_token";
            var newRefreshTokenValue = "new_refresh_token";

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(
                command.RefreshToken,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(user))
                .Returns(newAccessToken);

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshTokenValue);

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
                    !u.RefreshTokens.Any(rt => rt.Token.StartsWith("expired_token_") && !rt.IsRevoked)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

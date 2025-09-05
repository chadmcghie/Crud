using App.Abstractions;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tests.Unit.Backend.Application.Features.Authentication;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _mockLogger;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockLogger = new Mock<ILogger<RegisterUserCommandHandler>>();

        _handler = new RegisterUserCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object,
            _mockLogger.Object);
    }

    public class Handle : RegisterUserCommandHandlerTests
    {
        [Fact]
        public async Task WithValidData_ShouldCreateUserAndReturnTokens()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var hashedPassword = "hashed_password_value_with_proper_length_for_bcrypt";
            var accessToken = "access_token_value";
            var refreshTokenValue = "refresh_token_value";

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHasher.Setup(x => x.HashPassword(command.Password))
                .Returns(hashedPassword);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns(accessToken);

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshTokenValue);

            _mockUserRepository.Setup(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken ct) => u);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AccessToken.Should().Be(accessToken);
            result.RefreshToken.Should().Be(refreshTokenValue);
            result.Email.Should().Be(command.Email);
            result.UserId.Should().NotBeEmpty();

            _mockUserRepository.Verify(x => x.AddAsync(
                It.Is<User>(u =>
                    u.Email.Value == command.Email &&
                    u.FirstName == command.FirstName &&
                    u.LastName == command.LastName &&
                    u.PasswordHash.Value == hashedPassword &&
                    u.RefreshTokens.Count == 1),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _mockPasswordHasher.Verify(x => x.HashPassword(command.Password), Times.Once);
            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Once);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }

        [Fact]
        public async Task WithExistingEmail_ShouldReturnFailure()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "existing@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            var existingUser = User.Create(
                new Email(command.Email),
                new PasswordHash("existing_hash_with_proper_length_for_bcrypt"),
                command.FirstName,
                command.LastName);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Email already exists");
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();

            _mockUserRepository.Verify(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
                Times.Never);

            _mockPasswordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _mockJwtTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task WithInvalidEmail_ShouldReturnFailure()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "invalid-email",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("format is invalid");

            _mockUserRepository.Verify(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));

            _mockUserRepository.Verify(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHasher.Setup(x => x.HashPassword(command.Password))
                .Returns("hashed_password_with_proper_length_for_bcrypt");

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("access_token");

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            _mockUserRepository.Setup(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));

            exception.Message.Should().Be("Database error");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task WithInvalidPassword_ShouldReturnFailure(string? password)
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = password!,
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Password");

            _mockUserRepository.Verify(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData("", "Doe")]
        [InlineData("John", "")]
        [InlineData(null, "Doe")]
        [InlineData("John", null)]
        public async Task WithInvalidNames_ShouldReturnFailure(string? firstName, string? lastName)
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = firstName!,
                LastName = lastName!
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().ContainAny("First name", "Last name");

            _mockUserRepository.Verify(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WithCancellationRequested_ShouldCancelOperation()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
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
    }
}

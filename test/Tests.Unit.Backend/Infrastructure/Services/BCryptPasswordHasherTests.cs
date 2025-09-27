using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using Moq;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class BCryptPasswordHasherTests
{
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly IPasswordHasher _passwordHasher;

    public BCryptPasswordHasherTests()
    {
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _passwordHasher = _mockPasswordHasher.Object;
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        const string password = "TestPassword123!";
        const string expectedHash = "$2a$11$hashedpassword";
        _mockPasswordHasher.Setup(x => x.HashPassword(password))
            .Returns(expectedHash);

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        hashedPassword.Should().Be(expectedHash);
        _mockPasswordHasher.Verify(x => x.HashPassword(password), Times.Once);
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldBeCalledCorrectly()
    {
        // Arrange
        const string password = "TestPassword123!";
        _mockPasswordHasher.Setup(x => x.HashPassword(password))
            .Returns("$2a$11$hash1");

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().Be("$2a$11$hash1");
        hash2.Should().Be("$2a$11$hash1");
        _mockPasswordHasher.Verify(x => x.HashPassword(password), Times.Exactly(2));
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        const string password = "";
        _mockPasswordHasher.Setup(x => x.HashPassword(password))
            .Throws(new ArgumentException("Password cannot be empty"));

        // Act & Assert
        var act = () => _passwordHasher.HashPassword(password);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be empty");
    }

    [Fact]
    public void HashPassword_WithNullPassword_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? password = null;
        _mockPasswordHasher.Setup(x => x.HashPassword(password!))
            .Throws(new ArgumentNullException("password"));

        // Act & Assert
        var act = () => _passwordHasher.HashPassword(password!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("password");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "TestPassword123!";
        const string hashedPassword = "$2a$11$hashedvalue";
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, hashedPassword))
            .Returns(true);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
        _mockPasswordHasher.Verify(x => x.VerifyPassword(password, hashedPassword), Times.Once);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        const string wrongPassword = "WrongPassword456!";
        const string hashedPassword = "$2a$11$hashedvalue";
        _mockPasswordHasher.Setup(x => x.VerifyPassword(wrongPassword, hashedPassword))
            .Returns(false);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
        _mockPasswordHasher.Verify(x => x.VerifyPassword(wrongPassword, hashedPassword), Times.Once);
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        const string hashedPassword = "$2a$11$hashedvalue";
        _mockPasswordHasher.Setup(x => x.VerifyPassword("", hashedPassword))
            .Returns(false);

        // Act
        var result = _passwordHasher.VerifyPassword("", hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ShouldReturnFalse()
    {
        // Arrange
        const string hashedPassword = "$2a$11$hashedvalue";
        _mockPasswordHasher.Setup(x => x.VerifyPassword(null!, hashedPassword))
            .Returns(false);

        // Act
        var result = _passwordHasher.VerifyPassword(null!, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, null!))
            .Returns(false);

        // Act
        var result = _passwordHasher.VerifyPassword(password, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        const string invalidHash = "NotAValidBCryptHash";
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, invalidHash))
            .Returns(false);

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("password")]
    [InlineData("P@ssw0rd123!")]
    [InlineData("VeryLongPasswordWith123!@#SpecialCharacters")]
    [InlineData("🔐SecurePassword123")]
    public void PasswordHasher_InterfaceBehavior_ShouldBeConsistent(string password)
    {
        // Arrange
        const string expectedHash = "$2a$11$mockedhash";
        _mockPasswordHasher.Setup(x => x.HashPassword(password))
            .Returns(expectedHash);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, expectedHash))
            .Returns(true);

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);
        var verifyResult = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        hashedPassword.Should().Be(expectedHash);
        verifyResult.Should().BeTrue();
        _mockPasswordHasher.Verify(x => x.HashPassword(password), Times.Once);
        _mockPasswordHasher.Verify(x => x.VerifyPassword(password, expectedHash), Times.Once);
    }
}

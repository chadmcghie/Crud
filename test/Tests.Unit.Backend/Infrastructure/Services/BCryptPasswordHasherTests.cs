using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Services;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class BCryptPasswordHasherTests
{
    private readonly IPasswordHasher _passwordHasher;

    public BCryptPasswordHasherTests()
    {
        _passwordHasher = new BCryptPasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        const string password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2");
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
    {
        // Arrange
        const string password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        const string password = "";

        // Act & Assert
        var act = () => _passwordHasher.HashPassword(password);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be empty*");
    }

    [Fact]
    public void HashPassword_WithNullPassword_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? password = null;

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
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        const string wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword("", hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

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
    public void HashPassword_WithVariousPasswords_ShouldHashAndVerifyCorrectly(string password)
    {
        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);
        var verifyResult = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        verifyResult.Should().BeTrue();
    }
}
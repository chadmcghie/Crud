using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using App.Abstractions;
using Domain.Entities.Authentication;
using Domain.ValueObjects;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class JwtTokenServiceTests
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly User _testUser;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "ThisIsAVerySecureSecretKeyThatIsAtLeast256BitsLong!!!"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenExpirationMinutes", "15"},
            {"Jwt:RefreshTokenExpirationDays", "7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtTokenService = new JwtTokenService(_configuration);

        _testUser = new User(
            new Email("test@example.com"),
            new PasswordHash("$2a$12$dummyHashForTestingPurposesOnly.1234567890")
        );
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        // Act
        var token = _jwtTokenService.GenerateAccessToken(_testUser);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var tokenHandler = new JwtSecurityTokenHandler();
        var canRead = tokenHandler.CanReadToken(token);
        Assert.True(canRead);

        var jwtToken = tokenHandler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRequiredClaims()
    {
        // Act
        var token = _jwtTokenService.GenerateAccessToken(_testUser);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();

        Assert.Contains(claims, c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(claims, c => (c.Type == "email" || c.Type == ClaimTypes.Email) && c.Value == _testUser.Email.Value);
        Assert.Contains(claims, c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == "User");
        Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectExpiration()
    {
        // Act
        var token = _jwtTokenService.GenerateAccessToken(_testUser);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var expectedExpiration = DateTime.UtcNow.AddMinutes(15);
        Assert.True(Math.Abs((jwtToken.ValidTo - expectedExpiration).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectIssuerAndAudience()
    {
        // Act
        var token = _jwtTokenService.GenerateAccessToken(_testUser);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _jwtTokenService.GenerateAccessToken(null!));
        Assert.Equal("user", exception.ParamName);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);

        // Verify it's a valid base64 string
        var canConvert = Convert.TryFromBase64String(refreshToken, new byte[64], out _);
        Assert.True(canConvert);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _jwtTokenService.GenerateRefreshToken();
        var token2 = _jwtTokenService.GenerateRefreshToken();
        var token3 = _jwtTokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token1, token3);
        Assert.NotEqual(token2, token3);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var token = _jwtTokenService.GenerateAccessToken(_testUser);

        // Act
        var claimsPrincipal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(claimsPrincipal);
        Assert.NotNull(claimsPrincipal.Identity);
        Assert.True(claimsPrincipal.Identity.IsAuthenticated);

        var emailClaim = claimsPrincipal.FindFirst(ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(_testUser.Email.Value, emailClaim.Value);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        const string invalidToken = "invalid.token.here";

        // Act
        var claimsPrincipal = _jwtTokenService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(claimsPrincipal);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange - Create an expired token manually
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = System.Text.Encoding.ASCII.GetBytes("ThisIsAVerySecureSecretKeyThatIsAtLeast256BitsLong!!!");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com")
            }),
            Expires = DateTime.UtcNow.AddMinutes(-10), // Expired 10 minutes ago
            NotBefore = DateTime.UtcNow.AddMinutes(-20), // Valid from 20 minutes ago
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Act
        var claimsPrincipal = _jwtTokenService.ValidateToken(tokenString);

        // Assert
        Assert.Null(claimsPrincipal);
    }

    [Fact]
    public void ValidateToken_WithNullToken_ShouldReturnNull()
    {
        // Act
        var claimsPrincipal = _jwtTokenService.ValidateToken(null!);

        // Assert
        Assert.Null(claimsPrincipal);
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var claimsPrincipal = _jwtTokenService.ValidateToken("");

        // Assert
        Assert.Null(claimsPrincipal);
    }

    [Fact]
    public void GetTokenExpiry_WithValidToken_ShouldReturnCorrectExpiry()
    {
        // Arrange
        var token = _jwtTokenService.GenerateAccessToken(_testUser);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);

        // Act
        var expiry = _jwtTokenService.GetTokenExpiry(token);

        // Assert
        Assert.True(Math.Abs((expiry - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void GetTokenExpiry_WithInvalidToken_ShouldThrowException()
    {
        // Arrange
        const string invalidToken = "invalid.token.here";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _jwtTokenService.GetTokenExpiry(invalidToken));
    }

    [Fact]
    public void Configuration_WithMissingSettings_ShouldThrowException()
    {
        // Arrange
        var invalidSettings = new Dictionary<string, string?>
        {
            // Missing required settings
        };

        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidSettings)
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(invalidConfig));
    }
}

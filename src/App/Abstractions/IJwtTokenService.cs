using System.Security.Claims;
using Domain.Entities.Authentication;

namespace App.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiry(string token);
}

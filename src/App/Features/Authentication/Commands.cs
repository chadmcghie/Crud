using MediatR;

namespace App.Features.Authentication;

public class RegisterUserCommand : IRequest<AuthenticationResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class LoginCommand : IRequest<AuthenticationResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenCommand : IRequest<AuthenticationResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RevokeTokenCommand : IRequest<bool>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}

public class AuthenticationResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public List<string>? Roles { get; set; }
}
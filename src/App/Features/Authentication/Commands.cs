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

public class ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ResetPasswordCommand : IRequest<ResetPasswordResponse>
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ValidateResetTokenQuery : IRequest<ValidateResetTokenResponse>
{
    public string Token { get; set; } = string.Empty;
}

public class ValidateResetTokenResponse
{
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AddUserRoleCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;

    public AddUserRoleCommand(Guid userId, string role)
    {
        UserId = userId;
        Role = role;
    }
}

public class PromoteUserToAdminCommand : IRequest<AuthenticationResponse>
{
    public Guid UserId { get; set; }

    public PromoteUserToAdminCommand(string userId)
    {
        if (Guid.TryParse(userId, out var id))
            UserId = id;
    }
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

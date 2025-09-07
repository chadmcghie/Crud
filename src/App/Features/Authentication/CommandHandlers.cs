using App.Abstractions;
using Domain.Entities.Authentication;
using Domain.Events;
using Domain.Interfaces;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Features.Authentication;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthenticationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return new AuthenticationResponse { Success = false, Error = "First name is required" };

            if (string.IsNullOrWhiteSpace(request.LastName))
                return new AuthenticationResponse { Success = false, Error = "Last name is required" };

            // Create email value object (will throw if invalid)
            var email = new Email(request.Email);

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                return new AuthenticationResponse { Success = false, Error = "Email already exists" };
            }

            // Hash password
            var hashedPassword = _passwordHasher.HashPassword(request.Password);
            var passwordHash = new PasswordHash(hashedPassword);

            // Create new user
            var user = User.Create(email, passwordHash, request.FirstName, request.LastName);

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7); // Default refresh token expiry

            // Add refresh token to user
            user.AddRefreshToken(refreshToken, expiresAt);

            // Save user
            user = await _userRepository.AddAsync(user, cancellationToken);

            _logger.LogInformation("User registered successfully: {UserId}", user.Id);

            return new AuthenticationResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email.Value,
                Roles = user.Roles.ToList()
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning(ex, "Validation failed during registration: {Errors}", errors);
            return new AuthenticationResponse { Success = false, Error = errors };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input during registration");
            return new AuthenticationResponse { Success = false, Error = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            throw;
        }
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthenticationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Validate password
            if (string.IsNullOrWhiteSpace(request.Password))
                return new AuthenticationResponse { Success = false, Error = "Password is required" };

            // Create email value object
            var email = new Email(request.Email);

            // Find user
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                // Logging email even when masked can still expose sensitive info, so we omit it
                _logger.LogWarning("Login attempt with non-existent email address"); // No sensitive data logged
                return new AuthenticationResponse { Success = false, Error = "Invalid email or password" };
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                _logger.LogWarning("Login attempt on locked account: {UserId}", user.Id);
                return new AuthenticationResponse { Success = false, Error = "Account is locked" };
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash.Value))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {UserId}", user.Id);
                return new AuthenticationResponse { Success = false, Error = "Invalid email or password" };
            }

            // Cleanup expired tokens
            user.CleanupExpiredTokens();

            // Generate new tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7); // Default refresh token expiry

            // Add refresh token to user
            user.AddRefreshToken(refreshToken, expiresAt);

            // Update user
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

            return new AuthenticationResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email.Value,
                Roles = user.Roles.ToList()
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning(ex, "Validation failed during login: {Errors}", errors);
            return new AuthenticationResponse { Success = false, Error = errors };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input during login");
            return new AuthenticationResponse { Success = false, Error = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            throw;
        }
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validate input - add back basic validation for tests
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return new AuthenticationResponse { Success = false, Error = "Refresh token is required" };

        try
        {
            // Find user by refresh token
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Refresh token attempt with invalid token");
                return new AuthenticationResponse { Success = false, Error = "Invalid refresh token" };
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                _logger.LogWarning("Refresh token attempt on locked account: {UserId}", user.Id);
                return new AuthenticationResponse { Success = false, Error = "Account is locked" };
            }

            // Get the refresh token
            var refreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found for user: {UserId}", user.Id);
                return new AuthenticationResponse { Success = false, Error = "Invalid refresh token" };
            }

            // Check if token is revoked
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Attempt to use revoked refresh token for user: {UserId}", user.Id);
                return new AuthenticationResponse { Success = false, Error = "Refresh token has been revoked" };
            }

            // Check if token is expired
            if (refreshToken.IsExpired)
            {
                _logger.LogWarning("Attempt to use expired refresh token for user: {UserId}", user.Id);
                return new AuthenticationResponse { Success = false, Error = "Refresh token has expired" };
            }

            // Revoke the old refresh token
            user.RevokeRefreshToken(request.RefreshToken);

            // Cleanup expired tokens
            user.CleanupExpiredTokens();

            // Generate new tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7); // Default refresh token expiry

            // Add new refresh token to user
            user.AddRefreshToken(newRefreshToken, expiresAt);

            // Update user
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Refresh token rotated successfully for user: {UserId}", user.Id);

            return new AuthenticationResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                Email = user.Email.Value,
                Roles = user.Roles.ToList()
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning(ex, "Validation failed during token refresh: {Errors}", errors);
            return new AuthenticationResponse { Success = false, Error = errors };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }
}

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        IUserRepository userRepository,
        ILogger<RevokeTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validation is now handled by FluentValidation pipeline behavior

        try
        {
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Revoke token attempt with invalid token");
                return false;
            }

            var result = user.RevokeRefreshToken(request.RefreshToken);
            if (result)
            {
                await _userRepository.UpdateAsync(user, cancellationToken);
                _logger.LogInformation("Refresh token revoked for user: {UserId}", user.Id);
            }

            return result;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning(ex, "Validation failed during token revocation: {Errors}", errors);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            throw;
        }
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validation is now handled by FluentValidation pipeline behavior

        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Logout attempt with invalid user ID: {UserId}", request.UserId);
                return false;
            }

            // Revoke all active refresh tokens
            foreach (var token in user.RefreshTokens.Where(rt => rt.IsActive))
            {
                user.RevokeRefreshToken(token.Token);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("User logged out successfully: {UserId}", user.Id);

            return true;
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning(ex, "Validation failed during logout: {Errors}", errors);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Error = "Please provide a valid email address."
                };
            }

            // Try to create Email value object to validate format
            Email email;
            try
            {
                email = new Email(request.Email);
            }
            catch (ArgumentException)
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Error = "Please provide a valid email address."
                };
            }

            // Look up user by email
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            // If user doesn't exist, still return success for security reasons
            // This prevents email enumeration attacks
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent user"); // email omitted for privacy
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If the email exists in our system, password reset instructions have been sent."
                };
            }

            // Log the password reset request
            _logger.LogInformation("Password reset requested for user: {UserId}", user.Id);

            try
            {
                // Create password reset token
                var resetToken = PasswordResetToken.Create(user.Id);

                // Save token to database (this also invalidates any existing tokens)
                await _tokenRepository.AddAsync(resetToken, cancellationToken);

                // Send password reset email
                await _emailService.SendPasswordResetEmailAsync(request.Email, resetToken.Token, cancellationToken);

                _logger.LogInformation("Password reset email sent successfully for user: {UserId}", user.Id);

                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If the email exists in our system, password reset instructions have been sent."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process password reset for user: {UserId}", user.Id);

                return new ForgotPasswordResponse
                {
                    Success = false,
                    Error = "We're unable to process your request at this time. Please try again later."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during forgot password request");

            return new ForgotPasswordResponse
            {
                Success = false,
                Error = "We're unable to send the password reset email at this time. Please try again later."
            };
        }
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Validate token
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "Token is required"
                };
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "Password is required"
                };
            }

            // Password strength validation
            if (!IsPasswordStrong(request.NewPassword, out string passwordError))
            {
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = passwordError
                };
            }

            // Get the token from database
            var resetToken = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

            if (resetToken == null)
            {
                _logger.LogWarning("Password reset attempted with invalid token");
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "Invalid or expired password reset link."
                };
            }

            // Check if token is expired
            if (resetToken.IsExpired)
            {
                _logger.LogWarning("Password reset attempted with expired token for user: {UserId}", resetToken.UserId);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "The password reset link has expired. Please request a new one."
                };
            }

            // Check if token has already been used
            if (resetToken.IsUsed)
            {
                _logger.LogWarning("Password reset attempted with already used token for user: {UserId}", resetToken.UserId);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "This password reset link has already been used."
                };
            }

            // Get the user
            var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogError("User not found for password reset token: {UserId}", resetToken.UserId);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "User account not found."
                };
            }

            // Hash the new password
            var hashedPassword = _passwordHasher.HashPassword(request.NewPassword);

            // Update user's password
            user.UpdatePassword(new PasswordHash(hashedPassword));
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Mark token as used
            resetToken.MarkAsUsed();
            await _tokenRepository.UpdateAsync(resetToken, cancellationToken);
            await _tokenRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successful for user: {UserId}, Email: {Email}",
                user.Id, user.Email.Value);

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Your password has been reset successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password reset");

            return new ResetPasswordResponse
            {
                Success = false,
                Error = "We're unable to process your request at this time. Please try again later."
            };
        }
    }

    private bool IsPasswordStrong(string password, out string error)
    {
        error = string.Empty;

        if (password.Length < 8)
        {
            error = "Password must be at least 8 characters long";
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            error = "Password must contain at least one uppercase letter";
            return false;
        }

        if (!password.Any(char.IsLower))
        {
            error = "Password must contain at least one lowercase letter";
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            error = "Password must contain at least one number";
            return false;
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            error = "Password must contain at least one special character";
            return false;
        }

        return true;
    }
}

public class ValidateResetTokenQueryHandler : IRequestHandler<ValidateResetTokenQuery, ValidateResetTokenResponse>
{
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly ILogger<ValidateResetTokenQueryHandler> _logger;

    public ValidateResetTokenQueryHandler(
        IPasswordResetTokenRepository tokenRepository,
        ILogger<ValidateResetTokenQueryHandler> logger)
    {
        _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidateResetTokenResponse> Handle(ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Return invalid for empty tokens
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    IsExpired = false,
                    IsUsed = false,
                    ExpiresAt = null
                };
            }

            _logger.LogInformation("Token validation requested for token: {TokenPrefix}***",
                request.Token.Length > 10 ? request.Token.Substring(0, 10) : "short");

            // Get the token from database
            var resetToken = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

            if (resetToken == null)
            {
                _logger.LogWarning("Token validation failed - token not found");
                return new ValidateResetTokenResponse
                {
                    IsValid = false,
                    IsExpired = false,
                    IsUsed = false,
                    ExpiresAt = null
                };
            }

            var response = new ValidateResetTokenResponse
            {
                IsValid = resetToken.IsValid,
                IsExpired = resetToken.IsExpired,
                IsUsed = resetToken.IsUsed,
                ExpiresAt = resetToken.ExpiresAt
            };

            _logger.LogInformation("Token validation completed - Valid: {IsValid}, Expired: {IsExpired}, Used: {IsUsed}",
                response.IsValid, response.IsExpired, response.IsUsed);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");

            return new ValidateResetTokenResponse
            {
                IsValid = false,
                IsExpired = false,
                IsUsed = false,
                ExpiresAt = null
            };
        }
    }
}

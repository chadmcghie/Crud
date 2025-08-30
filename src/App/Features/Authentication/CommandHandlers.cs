using App.Abstractions;
using Domain.Entities.Authentication;
using Domain.Events;
using Domain.Interfaces;
using Domain.ValueObjects;
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

        // Validate basic input
        if (string.IsNullOrWhiteSpace(request.Email))
            return new AuthenticationResponse { Success = false, Error = "Email is required" };
        
        if (string.IsNullOrWhiteSpace(request.Password))
            return new AuthenticationResponse { Success = false, Error = "Password is required" };
        
        if (string.IsNullOrWhiteSpace(request.FirstName))
            return new AuthenticationResponse { Success = false, Error = "First name is required" };
        
        if (string.IsNullOrWhiteSpace(request.LastName))
            return new AuthenticationResponse { Success = false, Error = "Last name is required" };

        try
        {
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

        // Validate basic input
        if (string.IsNullOrWhiteSpace(request.Email))
            return new AuthenticationResponse { Success = false, Error = "Email is required" };
        
        if (string.IsNullOrWhiteSpace(request.Password))
            return new AuthenticationResponse { Success = false, Error = "Password is required" };

        try
        {
            // Create email value object
            var email = new Email(request.Email);

            // Find user
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
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

        // Validate input
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

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return false;

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

        if (request.UserId == Guid.Empty)
            return false;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }
}
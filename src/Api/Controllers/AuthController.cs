using System.Security.Claims;
using App.Features.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                SetRefreshTokenCookie(result.RefreshToken!);
                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresIn = 900 // 15 minutes in seconds
                });
            }

            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                SetRefreshTokenCookie(result.RefreshToken!);
                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresIn = 900 // 15 minutes in seconds
                });
            }

            // Return BadRequest for validation errors (format issues)
            if (result.Error?.Contains("format is invalid", StringComparison.OrdinalIgnoreCase) == true ||
                result.Error?.Contains("is required", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new { error = result.Error });
            }

            // Return Unauthorized for authentication failures
            return Unauthorized(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // If refresh token not in body, try to get from cookie
            if (string.IsNullOrEmpty(command?.RefreshToken))
            {
                var cookieToken = Request.Cookies["refreshToken"];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    command = new RefreshTokenCommand { RefreshToken = cookieToken };
                }
                else
                {
                    return BadRequest(new { error = "Refresh token is required" });
                }
            }

            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                SetRefreshTokenCookie(result.RefreshToken!);
                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresIn = 900 // 15 minutes in seconds
                });
            }

            return Unauthorized(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { error = "An error occurred during token refresh" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "Invalid user" });
            }

            // Create logout command
            var command = new LogoutCommand { UserId = Guid.Parse(userId) };
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                // Clear the refresh token cookie
                Response.Cookies.Delete("refreshToken");
                return Ok(new { message = "Logged out successfully" });
            }

            return BadRequest(new { error = "Failed to logout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "An error occurred during logout" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Roles = roles
        });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in production (requires HTTPS)
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7) // Refresh token validity
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}

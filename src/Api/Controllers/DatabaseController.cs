using Microsoft.AspNetCore.Mvc;
using Infrastructure.Data;
using Infrastructure.Services;
using System.Net;

namespace Api.Controllers;

/// <summary>
/// Database management controller for testing purposes only.
/// This controller is only available in Development and Testing environments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly DatabaseTestService _databaseTestService;
    private readonly ILogger<DatabaseController> _logger;
    private readonly IWebHostEnvironment _environment;

    public DatabaseController(
        DatabaseTestService databaseTestService,
        ILogger<DatabaseController> logger,
        IWebHostEnvironment environment)
    {
        _databaseTestService = databaseTestService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Resets the database to a clean state using Respawn.
    /// Only available in Development and Testing environments.
    /// Requires authentication token for security.
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetDatabase([FromBody] DatabaseResetRequest request)
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound(); // Return 404 to hide the endpoint in production
        }

        // Additional security: Check for authorization token
        var testToken = Request.Headers["X-Test-Reset-Token"].FirstOrDefault();
        var expectedToken = Environment.GetEnvironmentVariable("TEST_RESET_TOKEN") ?? "test-only-token";
        
        if (string.IsNullOrEmpty(testToken) || testToken != expectedToken)
        {
            _logger.LogWarning("Unauthorized database reset attempt from {RemoteIp}", 
                Request.HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { Error = "Invalid or missing test reset token" });
        }

        // Additional security: Only allow from localhost in Testing environment
        // Exception: In CI environments, allow any IP due to container networking
        if (_environment.EnvironmentName == "Testing" && 
            Environment.GetEnvironmentVariable("CI") != "true")
        {
            var remoteIp = Request.HttpContext.Connection.RemoteIpAddress;
            if (remoteIp != null && !IPAddress.IsLoopback(remoteIp))
            {
                _logger.LogWarning("Database reset attempt from non-localhost address: {RemoteIp}", remoteIp);
                return Forbid("Database reset is only allowed from localhost in Testing environment");
            }
        }
        else if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            _logger.LogInformation("CI environment detected - allowing database reset from any IP");
        }

        try
        {
            await _databaseTestService.ResetDatabaseAsync(request.WorkerIndex);
            
            return Ok(new { 
                Message = "Database reset successfully", 
                WorkerIndex = request.WorkerIndex,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database for worker {WorkerIndex}", request.WorkerIndex);
            return StatusCode(500, new { 
                Error = "Failed to reset database", 
                Details = ex.Message,
                WorkerIndex = request.WorkerIndex
            });
        }
    }

    /// <summary>
    /// Seeds the database with initial test data.
    /// Only available in Development and Testing environments.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase([FromBody] DatabaseSeedRequest request)
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound(); // Return 404 to hide the endpoint in production
        }

        try
        {
            await _databaseTestService.SeedDatabaseAsync(request.WorkerIndex);
            
            return Ok(new { 
                Message = "Database seeded successfully", 
                WorkerIndex = request.WorkerIndex,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database for worker {WorkerIndex}", request.WorkerIndex);
            return StatusCode(500, new { 
                Error = "Failed to seed database", 
                Details = ex.Message,
                WorkerIndex = request.WorkerIndex
            });
        }
    }

    /// <summary>
    /// Gets database status information for debugging.
    /// Only available in Development and Testing environments.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetDatabaseStatus()
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound();
        }

        try
        {
            var stats = await _databaseTestService.GetDatabaseStatsAsync();
            
            var status = new
            {
                Environment = _environment.EnvironmentName,
                ConnectionString = stats.ConnectionString,
                CanConnect = stats.CanConnect,
                PeopleCount = stats.PeopleCount,
                RolesCount = stats.RolesCount,
                WallsCount = stats.WallsCount,
                WindowsCount = stats.WindowsCount,
                Timestamp = stats.Timestamp
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database status");
            return StatusCode(500, new { Error = "Failed to get database status", Details = ex.Message });
        }
    }

    /// <summary>
    /// Validates the database state before test execution.
    /// Only available in Development and Testing environments.
    /// </summary>
    [HttpGet("validate-pre-test")]
    public async Task<IActionResult> ValidatePreTestState([FromQuery] int workerIndex)
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound();
        }

        try
        {
            var validation = await _databaseTestService.ValidatePreTestStateAsync(workerIndex);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate pre-test database state for worker {WorkerIndex}", workerIndex);
            return StatusCode(500, new { Error = "Failed to validate database state", Details = ex.Message });
        }
    }

    /// <summary>
    /// Validates the database state after test execution.
    /// Only available in Development and Testing environments.
    /// </summary>
    [HttpGet("validate-post-test")]
    public async Task<IActionResult> ValidatePostTestState([FromQuery] int workerIndex)
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound();
        }

        try
        {
            var validation = await _databaseTestService.ValidatePostTestStateAsync(workerIndex);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate post-test database state for worker {WorkerIndex}", workerIndex);
            return StatusCode(500, new { Error = "Failed to validate database state", Details = ex.Message });
        }
    }

    /// <summary>
    /// Performs database integrity verification.
    /// Only available in Development and Testing environments.
    /// </summary>
    [HttpGet("verify-integrity")]
    public async Task<IActionResult> VerifyDatabaseIntegrity([FromQuery] int workerIndex)
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound();
        }

        try
        {
            var isValid = await _databaseTestService.VerifyDatabaseIntegrityAsync(workerIndex);
            return Ok(new { 
                IsValid = isValid,
                WorkerIndex = workerIndex,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify database integrity for worker {WorkerIndex}", workerIndex);
            return StatusCode(500, new { Error = "Failed to verify database integrity", Details = ex.Message });
        }
    }


}

/// <summary>
/// Request model for database reset operation
/// </summary>
public class DatabaseResetRequest
{
    public int WorkerIndex { get; set; }
    public bool PreserveSchema { get; set; } = true;
}

/// <summary>
/// Request model for database seed operation
/// </summary>
public class DatabaseSeedRequest
{
    public int WorkerIndex { get; set; }
}

using Microsoft.AspNetCore.Mvc;
using Infrastructure.Data;
using Infrastructure.Services;

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
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetDatabase([FromBody] DatabaseResetRequest request)
    {
        // Security check - only allow in non-production environments
        if (!_environment.IsDevelopment() && _environment.EnvironmentName != "Testing")
        {
            return NotFound(); // Return 404 to hide the endpoint in production
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

using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Controllers;

/// <summary>
/// API health endpoint for detailed application health status
/// Provides environment-specific health information for smoke testing
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public HealthController(
      HealthCheckService healthCheckService,
      ApplicationDbContext dbContext,
      IWebHostEnvironment environment,
      IConfiguration configuration)
    {
        _healthCheckService = healthCheckService;
        _dbContext = dbContext;
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets detailed API health status including database connectivity and environment information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            // Run health checks
            var healthReport = await _healthCheckService.CheckHealthAsync();

            // Test database connectivity
            var canConnectToDatabase = await TestDatabaseConnectivity();

            // Gather environment information
            var healthResponse = new
            {
                Status = healthReport.Status.ToString(),
                Environment = _environment.EnvironmentName,
                DatabaseProvider = _configuration.GetValue<string>("DatabaseProvider") ?? "SQLite",
                DatabaseConnectivity = canConnectToDatabase ? "Connected" : "Disconnected",
                Timestamp = DateTime.UtcNow,
                Checks = healthReport.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    Status = kvp.Value.Status.ToString(),
                    Duration = kvp.Value.Duration.TotalMilliseconds,
                    Description = kvp.Value.Description
                }
              ),
                Application = new
                {
                    Name = "CRUD API",
                    Version = "1.0.0",
                    Framework = ".NET 8"
                }
            };

            // Return appropriate status code based on health
            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => 200,
                HealthStatus.Degraded => 200, // Still operational
                HealthStatus.Unhealthy => 503,
                _ => 503
            };

            return StatusCode(statusCode, healthResponse);
        }
        catch (Exception ex)
        {
            // Return error response for unhandled exceptions
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Environment = _environment.EnvironmentName,
                Error = "Health check failed",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Quick health check endpoint for load balancers and monitoring
    /// </summary>
    [HttpGet("quick")]
    public IActionResult GetQuickHealth()
    {
        try
        {
            return Ok(new
            {
                Status = "Healthy",
                Environment = _environment.EnvironmentName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch
        {
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Environment = _environment.EnvironmentName,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Tests database connectivity without performing complex operations
    /// </summary>
    private async Task<bool> TestDatabaseConnectivity()
    {
        try
        {
            // Simple database connectivity test
            await _dbContext.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

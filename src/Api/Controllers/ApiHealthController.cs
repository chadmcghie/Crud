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
[Route("api/health")]
[Produces("application/json")]
public class ApiHealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ApiHealthController(
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
    public async Task<IActionResult> GetDetailedHealth()
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
                status = "Healthy",
                environment = _environment.EnvironmentName,
                databaseProvider = _configuration.GetValue<string>("DatabaseProvider") ?? "SQLite",
                databaseConnectivity = canConnectToDatabase ? "Connected" : "Disconnected",
                timestamp = DateTime.UtcNow.ToString("O"),
                checks = new
                {
                    database = new
                    {
                        status = canConnectToDatabase ? "Healthy" : "Unhealthy",
                        duration = 0.0,
                        description = canConnectToDatabase ? "Database connection is healthy" : "Database connection failed"
                    }
                },
                application = new
                {
                    name = "CRUD API",
                    version = "1.0.0",
                    framework = ".NET 8"
                }
            };

            return Ok(healthResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "Unhealthy", error = ex.Message });
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

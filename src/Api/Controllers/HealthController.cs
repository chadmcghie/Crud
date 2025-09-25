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
    /// Gets simple health status for basic health checks
    /// Returns plain text for E2E test compatibility
    /// </summary>
    [HttpGet]
    [Produces("text/plain")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            // Test database connectivity
            var canConnectToDatabase = await TestDatabaseConnectivity();

            if (canConnectToDatabase)
            {
                return Ok("Healthy");
            }
            else
            {
                return StatusCode(503, "Unhealthy");
            }
        }
        catch (Exception)
        {
            return StatusCode(503, "Unhealthy");
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

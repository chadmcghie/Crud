using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Api.Controllers;

/// <summary>
/// System information endpoint for E2E testing and monitoring
/// Provides environment-specific information for smoke tests
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public SystemController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ApplicationDbContext dbContext)
    {
        _environment = environment;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets system information including environment and database provider
    /// Used by smoke tests to validate testing configuration
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetSystemInfo()
    {
        try
        {
            // Determine database provider
            var databaseProvider = GetDatabaseProvider();

            // Test database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync();

            var systemInfo = new
            {
                Environment = _environment.EnvironmentName,
                DatabaseProvider = databaseProvider,
                DatabaseConnected = canConnect,
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId
            };

            return Ok(systemInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = "Failed to retrieve system information",
                Message = ex.Message,
                Environment = _environment.EnvironmentName,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Determines the database provider from the connection string
    /// </summary>
    private string GetDatabaseProvider()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                return "Unknown";
            }

            // SQLite detection
            if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                return "SQLite";
            }

            // SQL Server detection
            if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return "SqlServer";
            }

            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using App.Abstractions;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(IWebHostEnvironment environment, IDatabaseTestService databaseTestService) : ControllerBase
{
    [HttpGet("environment")]
    public IActionResult GetEnvironment()
    {
        return Ok(new
        {
            EnvironmentName = environment.EnvironmentName,
            IsTesting = environment.IsEnvironment("Testing"),
            IsDevelopment = environment.IsDevelopment(),
            IsProduction = environment.IsProduction(),
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("auth-test")]
    public IActionResult TestAuthBypass()
    {
        var isTesting = environment.IsEnvironment("Testing");
        return Ok(new
        {
            Message = isTesting
                ? "✅ Authorization should be bypassed in Testing environment"
                : "⚠️ Authorization should be enforced in non-Testing environment",
            Environment = environment.EnvironmentName,
            ShouldBypass = isTesting
        });
    }

    /// <summary>
    /// Reset database endpoint for E2E testing
    /// Only available in Testing environment
    /// </summary>
    [HttpPost("reset-database")]
    public async Task<IActionResult> ResetDatabase()
    {
        // Security check - only allow in Testing environment
        if (!environment.IsEnvironment("Testing"))
        {
            return NotFound(); // Hide the endpoint in non-testing environments
        }

        try
        {
            // Use the database test service to reset the database
            // Use worker index 0 for single-threaded E2E tests
            await databaseTestService.ResetDatabaseAsync(0, seedData: false);

            return Ok(new
            {
                Message = "Database reset successfully",
                Timestamp = DateTime.UtcNow,
                Environment = environment.EnvironmentName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = "Failed to reset database",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

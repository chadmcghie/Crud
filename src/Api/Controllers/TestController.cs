using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(IWebHostEnvironment environment) : ControllerBase
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
}

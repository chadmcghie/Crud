using Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

/// <summary>
/// Health check for database connectivity using dependency injection instead of BuildServiceProvider
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // Warm up EF Core by forcing model compilation
            // This ensures write operations are ready before tests begin
            _ = await _dbContext.People.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database ready for operations");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database not ready", ex);
        }
    }
}

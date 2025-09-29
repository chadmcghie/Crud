using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Adapter to make WebApplicationFactory work with ITestWebApplicationFactory interface
/// Used for smoke tests to integrate with existing authentication helpers
/// </summary>
public class SmokeTestFactoryAdapter : ITestWebApplicationFactory
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public SmokeTestFactoryAdapter(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    public HttpClient CreateClient() => _factory.CreateClient();

    public IServiceProvider Services => _factory.Services;

    public TestLogCapture? LogCapture => null; // Not implemented for smoke tests

    public void EnsureDatabaseCreated()
    {
        // Ensure database is created with fresh schema for smoke tests
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Force recreation to ensure we have the latest schema
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    public async Task ClearDatabaseAsync()
    {
        // For smoke tests, we create unique databases per test so clearing is optional
        await Task.CompletedTask;
    }

    public async Task SetUserRoleAsync(string email, string role)
    {
        // Enhanced implementation for smoke tests with better error handling
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        if (user != null)
        {
            // Add the requested role (not just Admin)
            user.AddRole(role);
            await dbContext.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Infrastructure.Data;

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
    // For smoke tests, database creation is handled by the factory configuration
  }

  public async Task ClearDatabaseAsync()
  {
    // For smoke tests, we create unique databases per test so clearing is optional
    await Task.CompletedTask;
  }

  public async Task SetUserRoleAsync(string email, string role)
  {
    // Basic implementation for smoke tests
    using var scope = Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user != null && role == "Admin")
    {
      user.AddRole("Admin");
      await dbContext.SaveChangesAsync();
    }
  }

  public void Dispose()
  {
    _factory.Dispose();
  }
}
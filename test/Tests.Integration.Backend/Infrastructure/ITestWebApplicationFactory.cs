using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Abstraction for test web application factory to decouple tests from specific infrastructure implementations
/// </summary>
public interface ITestWebApplicationFactory : IDisposable
{
    /// <summary>
    /// Creates an HTTP client for testing
    /// </summary>
    HttpClient CreateClient();

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Ensures the test database is created and ready for use
    /// </summary>
    void EnsureDatabaseCreated();

    /// <summary>
    /// Clears all data from the test database
    /// </summary>
    Task ClearDatabaseAsync();

    /// <summary>
    /// Gets the test log capture instance for debugging server-side errors (if available)
    /// </summary>
    TestLogCapture? LogCapture { get; }

    /// <summary>
    /// Sets a user's role for testing authorization scenarios
    /// </summary>
    Task SetUserRoleAsync(string email, string role);
}

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Provider interface for creating test web application factories
/// Allows for different implementations (SQLite, InMemory, etc.) to be configured via DI
/// </summary>
public interface ITestWebApplicationFactoryProvider
{
    /// <summary>
    /// Creates a new test web application factory instance
    /// </summary>
    ITestWebApplicationFactory CreateFactory();
}

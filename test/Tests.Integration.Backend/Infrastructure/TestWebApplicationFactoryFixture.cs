namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// xUnit fixture that provides a shared test web application factory instance
/// Uses dependency injection to configure the appropriate implementation
/// </summary>
public class TestWebApplicationFactoryFixture : IDisposable
{
    private readonly ITestWebApplicationFactory _factory;

    public TestWebApplicationFactoryFixture()
    {
        // For now, directly instantiate SQLite implementation
        // In the future, this could be configured via DI container or configuration
        _factory = new SqliteTestWebApplicationFactory();
    }

    public ITestWebApplicationFactory Factory => _factory;

    public void Dispose()
    {
        _factory?.Dispose();
    }
}

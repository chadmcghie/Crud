namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Provider for creating SQLite-based test web application factories
/// </summary>
public class SqliteTestWebApplicationFactoryProvider : ITestWebApplicationFactoryProvider
{
    public ITestWebApplicationFactory CreateFactory()
    {
        return new SqliteTestWebApplicationFactory();
    }
}

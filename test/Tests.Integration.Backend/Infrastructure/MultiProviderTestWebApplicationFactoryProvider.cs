namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Factory provider that creates test web application factories for different database providers
/// Supports testing the same scenarios across SQLite, InMemory, and SQL Server providers
/// </summary>
public static class MultiProviderTestWebApplicationFactoryProvider
{
    /// <summary>
    /// Creates a test factory for the specified database provider
    /// </summary>
    /// <param name="provider">The database provider to create a factory for</param>
    /// <returns>A configured test web application factory</returns>
    public static IMultiProviderTestWebApplicationFactory Create(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SQLite => new SqliteTestWebApplicationFactory(),
            DatabaseProvider.InMemory => new InMemoryTestWebApplicationFactory(),
            DatabaseProvider.SqlServer => new SqlServerTestWebApplicationFactory(),
            _ => throw new ArgumentException($"Unsupported database provider: {provider}", nameof(provider))
        };
    }

    /// <summary>
    /// Gets all supported database providers for comprehensive testing
    /// </summary>
    /// <returns>An enumerable of all supported providers</returns>
    public static IEnumerable<DatabaseProvider> GetAllProviders()
    {
        return Enum.GetValues<DatabaseProvider>();
    }

    /// <summary>
    /// Gets all supported database providers as test data for xUnit Theory tests
    /// </summary>
    /// <returns>Object arrays suitable for xUnit Theory InlineData or MemberData</returns>
    public static IEnumerable<object[]> GetAllProvidersAsTestData()
    {
        return GetAllProviders().Select(provider => new object[] { provider });
    }

    /// <summary>
    /// Gets providers that support specific features for targeted testing
    /// </summary>
    /// <param name="requiresTransactions">Whether the test requires transaction support</param>
    /// <param name="requiresForeignKeys">Whether the test requires foreign key constraint enforcement</param>
    /// <param name="requiresPersistence">Whether the test requires data persistence between operations</param>
    /// <returns>Providers that meet the specified requirements</returns>
    public static IEnumerable<DatabaseProvider> GetProvidersWithFeatures(
        bool requiresTransactions = false,
        bool requiresForeignKeys = false,
        bool requiresPersistence = false)
    {
        return GetAllProviders().Where(provider =>
        {
            using var factory = Create(provider);
            return (!requiresTransactions || factory.SupportsTransactions) &&
                   (!requiresForeignKeys || factory.SupportsForeignKeys) &&
                   (!requiresPersistence || factory.SupportsPersistence);
        });
    }

    /// <summary>
    /// Gets providers with specific features as test data for xUnit Theory tests
    /// </summary>
    public static IEnumerable<object[]> GetProvidersWithFeaturesAsTestData(
        bool requiresTransactions = false,
        bool requiresForeignKeys = false,
        bool requiresPersistence = false)
    {
        return GetProvidersWithFeatures(requiresTransactions, requiresForeignKeys, requiresPersistence)
            .Select(provider => new object[] { provider });
    }
}

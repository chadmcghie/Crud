namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Extended test factory interface that supports multiple database providers
/// for comprehensive database provider testing
/// </summary>
public interface IMultiProviderTestWebApplicationFactory : ITestWebApplicationFactory
{
    /// <summary>
    /// Gets the current database provider being used by this factory
    /// </summary>
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Gets a human-readable name for the current provider (for test naming)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Indicates whether this provider supports full transaction rollback
    /// </summary>
    bool SupportsTransactions { get; }

    /// <summary>
    /// Indicates whether this provider supports foreign key constraints
    /// </summary>
    bool SupportsForeignKeys { get; }

    /// <summary>
    /// Indicates whether this provider persists data between factory instances
    /// </summary>
    bool SupportsPersistence { get; }
}

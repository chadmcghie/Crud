namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Enumeration of supported database providers for multi-provider testing
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// SQLite database provider - file-based, lightweight, fast
    /// </summary>
    SQLite,

    /// <summary>
    /// In-Memory database provider - fastest, no persistence, ideal for unit-like tests
    /// </summary>
    InMemory,

    /// <summary>
    /// SQL Server database provider - full-featured, closest to production
    /// </summary>
    SqlServer
}

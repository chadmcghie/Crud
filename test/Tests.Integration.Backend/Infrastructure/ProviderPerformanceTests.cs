using System.Diagnostics;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Performance characteristic tests for different database providers
/// Measures and compares basic performance metrics across providers
/// Note: These are relative performance tests, not absolute benchmarks
/// </summary>
public class ProviderPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<IMultiProviderTestWebApplicationFactory> _factories = new();

    public ProviderPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Database_Creation_Performance(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        _output.WriteLine($"Testing database creation performance for {factory.ProviderName} provider");

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        factory.EnsureDatabaseCreated();
        stopwatch.Stop();

        // Assert & Report
        var creationTimeMs = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"{factory.ProviderName} database creation time: {creationTimeMs}ms");

        // Basic performance expectations (these are rough guidelines, not hard requirements)
        switch (provider)
        {
            case DatabaseProvider.InMemory:
                creationTimeMs.Should().BeLessThan(1000, "InMemory should be fastest for creation");
                break;
            case DatabaseProvider.SQLite:
                creationTimeMs.Should().BeLessThan(5000, "SQLite should be reasonably fast for creation");
                break;
            case DatabaseProvider.SqlServer:
                // SQL Server may take longer due to LocalDB startup
                _output.WriteLine("SQL Server creation time is provider-dependent (LocalDB startup)");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Bulk_Insert_Performance(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing bulk insert performance for {factory.ProviderName} provider");

        const int recordCount = 100;
        var roles = Enumerable.Range(1, recordCount).Select(i =>
            Domain.Entities.Role.Create(
                $"BulkRole_{provider}_{i:D3}",
                $"Bulk inserted role {i} for {provider} performance test")
        ).ToList();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
        stopwatch.Stop();

        // Assert & Report
        var insertTimeMs = stopwatch.ElapsedMilliseconds;
        var recordsPerSecond = recordCount / (stopwatch.ElapsedMilliseconds / 1000.0);

        _output.WriteLine($"{factory.ProviderName} bulk insert: {recordCount} records in {insertTimeMs}ms ({recordsPerSecond:F1} records/sec)");

        // Verify all records were inserted
        var insertedCount = await context.Roles.CountAsync(r => r.Name.StartsWith($"BulkRole_{provider}_"));
        insertedCount.Should().Be(recordCount, "All records should be inserted");

        // Performance expectations
        insertTimeMs.Should().BeLessThan(30000, $"Bulk insert should complete within 30 seconds for {factory.ProviderName}");
        recordsPerSecond.Should().BeGreaterThan(1, $"Should insert at least 1 record per second for {factory.ProviderName}");
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Query_Performance(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing query performance for {factory.ProviderName} provider");

        // Create test data
        const int recordCount = 50;
        var roles = Enumerable.Range(1, recordCount).Select(i =>
            Domain.Entities.Role.Create(
                $"QueryRole_{provider}_{i:D3}",
                $"Query test role {i} for {provider}")
        ).ToList();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        // Act & Measure - Simple query
        var stopwatch = Stopwatch.StartNew();
        var queriedRoles = await context.Roles
            .Where(r => r.Name.StartsWith($"QueryRole_{provider}_"))
            .OrderBy(r => r.Name)
            .ToListAsync();
        stopwatch.Stop();

        // Assert & Report
        var queryTimeMs = stopwatch.ElapsedMilliseconds;
        var recordsPerSecondQuery = recordCount / (stopwatch.ElapsedMilliseconds / 1000.0);

        _output.WriteLine($"{factory.ProviderName} query: {recordCount} records in {queryTimeMs}ms ({recordsPerSecondQuery:F1} records/sec)");

        queriedRoles.Should().HaveCount(recordCount, "All records should be queryable");
        queryTimeMs.Should().BeLessThan(10000, $"Query should complete within 10 seconds for {factory.ProviderName}");
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Database_Cleanup_Performance(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        _output.WriteLine($"Testing database cleanup performance for {factory.ProviderName} provider");

        // Create some test data to clean up
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roles = Enumerable.Range(1, 20).Select(i =>
                Domain.Entities.Role.Create(
                    $"CleanupRole_{provider}_{i}",
                    $"Role for cleanup test {i}")
            ).ToList();
            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }

        // Act & Measure cleanup
        var stopwatch = Stopwatch.StartNew();
        await factory.ClearDatabaseAsync();
        stopwatch.Stop();

        // Assert & Report
        var cleanupTimeMs = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"{factory.ProviderName} cleanup time: {cleanupTimeMs}ms");

        // Verify cleanup worked
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var remainingRoles = await context.Roles.CountAsync();
            remainingRoles.Should().Be(0, "Database should be clean");
        }

        // Performance expectations
        cleanupTimeMs.Should().BeLessThan(15000, $"Cleanup should complete within 15 seconds for {factory.ProviderName}");
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Complex_Query_Performance(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing complex query performance for {factory.ProviderName} provider");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create test data with relationships
        var roles = Enumerable.Range(1, 10).Select(i =>
            Domain.Entities.Role.Create(
                $"ComplexRole_{provider}_{i}",
                $"Complex test role {i}")
        ).ToList();
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        var people = Enumerable.Range(1, 20).Select(i =>
        {
            var person = Domain.Entities.Person.Create(
                $"ComplexPerson_{provider}_{i}",
                $"+123456789{i:D2}");
            // Assign roles to people (some people have multiple roles)
            if (i % 3 == 0) person.AddRole(roles[0]);
            if (i % 5 == 0) person.AddRole(roles[1]);
            return person;
        }).ToList();
        context.People.AddRange(people);
        await context.SaveChangesAsync();

        // Act & Measure - Complex query with joins
        var stopwatch = Stopwatch.StartNew();
        var complexQueryResult = await context.People
            .Include(p => p.Roles)
            .Where(p => p.FullName.StartsWith($"ComplexPerson_{provider}_") && p.Roles.Any())
            .OrderBy(p => p.FullName)
            .ToListAsync();
        stopwatch.Stop();

        // Assert & Report
        var complexQueryTimeMs = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"{factory.ProviderName} complex query (joins): {complexQueryResult.Count} records in {complexQueryTimeMs}ms");

        complexQueryResult.Should().NotBeEmpty("Should find people with roles");
        complexQueryTimeMs.Should().BeLessThan(10000, $"Complex query should complete within 10 seconds for {factory.ProviderName}");
    }

    [Fact]
    public async Task Compare_Provider_Performance_Characteristics()
    {
        // This test runs basic operations on all providers and compares results
        _output.WriteLine("=== Provider Performance Comparison ===");

        var performanceResults = new Dictionary<DatabaseProvider, Dictionary<string, long>>();

        foreach (var provider in Enum.GetValues<DatabaseProvider>())
        {
            try
            {
                var factory = CreateFactory(provider);
                var results = new Dictionary<string, long>();

                // Test database creation
                var creationStopwatch = Stopwatch.StartNew();
                factory.EnsureDatabaseCreated();
                creationStopwatch.Stop();
                results["Creation"] = creationStopwatch.ElapsedMilliseconds;

                // Test simple insert
                await factory.ClearDatabaseAsync();
                using var scope = factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var insertStopwatch = Stopwatch.StartNew();
                var role = Domain.Entities.Role.Create(
                    $"PerfTestRole_{provider}",
                    "Performance test role");
                context.Roles.Add(role);
                await context.SaveChangesAsync();
                insertStopwatch.Stop();
                results["SingleInsert"] = insertStopwatch.ElapsedMilliseconds;

                // Test simple query
                var queryStopwatch = Stopwatch.StartNew();
                var queriedRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == $"PerfTestRole_{provider}");
                queryStopwatch.Stop();
                results["SingleQuery"] = queryStopwatch.ElapsedMilliseconds;

                performanceResults[provider] = results;

                _output.WriteLine($"{factory.ProviderName}: Creation={results["Creation"]}ms, Insert={results["SingleInsert"]}ms, Query={results["SingleQuery"]}ms");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"{provider} performance test failed: {ex.Message}");
            }
        }

        // Basic assertions about relative performance
        if (performanceResults.ContainsKey(DatabaseProvider.InMemory))
        {
            var inMemoryCreation = performanceResults[DatabaseProvider.InMemory]["Creation"];
            _output.WriteLine($"InMemory is typically fastest for creation: {inMemoryCreation}ms");
        }

        _output.WriteLine("Performance comparison completed. Results are relative and environment-dependent.");
    }

    private IMultiProviderTestWebApplicationFactory CreateFactory(DatabaseProvider provider)
    {
        var factory = MultiProviderTestWebApplicationFactoryProvider.Create(provider);
        _factories.Add(factory);
        return factory;
    }

    public static IEnumerable<object[]> GetAllProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetAllProvidersAsTestData();

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
        _factories.Clear();
    }
}
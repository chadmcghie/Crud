using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Tests that verify behavior differences between database providers
/// Ensures our application works correctly regardless of the underlying database provider
/// </summary>
public class DatabaseProviderDifferenceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<IMultiProviderTestWebApplicationFactory> _factories = new();

    public DatabaseProviderDifferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_ShouldCreateDatabaseSuccessfully(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        _output.WriteLine($"Testing database creation for provider: {factory.ProviderName}");

        // Act & Assert
        var action = () => factory.EnsureDatabaseCreated();
        action.Should().NotThrow($"Database creation should succeed for {factory.ProviderName}");

        // Verify we can get a DbContext
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Should().NotBeNull();

        // Verify database is accessible
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue($"Should be able to connect to {factory.ProviderName} database");
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_ShouldClearDatabaseSuccessfully(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        _output.WriteLine($"Testing database clearing for provider: {factory.ProviderName}");

        // Add some test data first
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Add a simple role for testing
            var role = Domain.Entities.Role.Create("TestRole", "Test role for clearing");
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            // Verify data exists
            var roleCount = await context.Roles.CountAsync();
            roleCount.Should().BeGreaterThan(0, "Test data should be inserted");
        }

        // Act
        await factory.ClearDatabaseAsync();

        // Assert
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roleCount = await context.Roles.CountAsync();
            roleCount.Should().Be(0, $"Database should be cleared for {factory.ProviderName}");
        }
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_ShouldSupportBasicCrudOperations(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing basic CRUD operations for provider: {factory.ProviderName}");

        var testRoleName = $"TestRole_{provider}";
        var testRoleDescription = $"Test role for {provider} provider";

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert - Create
        var role = Domain.Entities.Role.Create(testRoleName, testRoleDescription);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        role.Id.Should().NotBeEmpty("Role should have been assigned an ID after save");

        // Act & Assert - Read
        var retrievedRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == testRoleName);
        retrievedRole.Should().NotBeNull($"Role should be retrievable from {provider} database");
        retrievedRole!.Description.Should().Be(testRoleDescription);

        // Act & Assert - Update
        var newDescription = $"Updated description for {provider}";
        retrievedRole.UpdateDescription(newDescription);
        await context.SaveChangesAsync();

        var updatedRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == testRoleName);
        updatedRole!.Description.Should().Be(newDescription, "Role should be updated");

        // Act & Assert - Delete
        context.Roles.Remove(updatedRole);
        await context.SaveChangesAsync();

        var deletedRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == testRoleName);
        deletedRole.Should().BeNull("Role should be deleted");
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionSupportingProviders_ShouldRollbackOnError(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing transaction rollback for provider: {factory.ProviderName}");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert
        var initialCount = await context.Roles.CountAsync();

        try
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            // Add a role
            var role = Domain.Entities.Role.Create("TransactionTestRole", "This should be rolled back");
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            // Verify it's in the context (but not committed)
            var roleInTransaction = await context.Roles.CountAsync();
            roleInTransaction.Should().Be(initialCount + 1, "Role should exist within transaction");

            // Rollback the transaction
            await transaction.RollbackAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Transaction test failed for {provider}: {ex.Message}");
            throw;
        }

        // Verify rollback worked
        var finalCount = await context.Roles.CountAsync();
        finalCount.Should().Be(initialCount, "Transaction should have been rolled back");
    }

    [Fact]
    public void InMemoryProvider_ShouldNotSupportTransactions()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.InMemory);
        _output.WriteLine("Verifying InMemory provider transaction limitations");

        // Assert
        factory.SupportsTransactions.Should().BeFalse("InMemory provider doesn't support real transactions");
        factory.SupportsForeignKeys.Should().BeFalse("InMemory provider doesn't enforce FK constraints");
        factory.SupportsPersistence.Should().BeFalse("InMemory provider doesn't persist data");
    }

    [Fact]
    public void SqliteProvider_ShouldSupportTransactionsAndConstraints()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.SQLite);
        _output.WriteLine("Verifying SQLite provider capabilities");

        // Assert
        factory.SupportsTransactions.Should().BeTrue("SQLite provider supports transactions");
        factory.SupportsForeignKeys.Should().BeTrue("SQLite provider supports FK constraints");
        factory.SupportsPersistence.Should().BeTrue("SQLite provider persists data to file");
    }

    [Fact]
    public void SqlServerProvider_ShouldSupportAllFeatures()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.SqlServer);
        _output.WriteLine("Verifying SQL Server provider capabilities");

        // Assert
        factory.SupportsTransactions.Should().BeTrue("SQL Server supports full ACID transactions");
        factory.SupportsForeignKeys.Should().BeTrue("SQL Server enforces FK constraints");
        factory.SupportsPersistence.Should().BeTrue("SQL Server persists data to disk");
    }

    private IMultiProviderTestWebApplicationFactory CreateFactory(DatabaseProvider provider)
    {
        var factory = MultiProviderTestWebApplicationFactoryProvider.Create(provider);
        _factories.Add(factory);
        return factory;
    }

    public static IEnumerable<object[]> GetAllProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetAllProvidersAsTestData();

    public static IEnumerable<object[]> GetTransactionSupportingProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetProvidersWithFeaturesAsTestData(requiresTransactions: true);

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
        _factories.Clear();
    }
}

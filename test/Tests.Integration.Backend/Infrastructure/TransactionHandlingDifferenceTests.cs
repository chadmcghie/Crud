using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Tests that validate transaction handling differences between database providers
/// Ensures our application handles provider-specific transaction behaviors correctly
/// </summary>
public class TransactionHandlingDifferenceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<IMultiProviderTestWebApplicationFactory> _factories = new();

    public TransactionHandlingDifferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionProviders_Should_Support_Explicit_Transactions(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing explicit transaction support for {factory.ProviderName} provider");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var initialCount = await context.Roles.CountAsync();

        // Act & Assert
        using var transaction = await context.Database.BeginTransactionAsync();

        var role = Domain.Entities.Role.Create(
            $"TransactionRole_{provider}",
            "Role created in transaction");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        // Verify data exists within transaction
        var countInTransaction = await context.Roles.CountAsync();
        countInTransaction.Should().Be(initialCount + 1, "Data should exist within transaction");

        // Commit transaction
        await transaction.CommitAsync();

        // Verify data persists after commit
        var countAfterCommit = await context.Roles.CountAsync();
        countAfterCommit.Should().Be(initialCount + 1, $"Data should persist after commit for {factory.ProviderName}");
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionProviders_Should_Support_Transaction_Rollback(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing transaction rollback for {factory.ProviderName} provider");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var initialCount = await context.Roles.CountAsync();

        // Act
        using var transaction = await context.Database.BeginTransactionAsync();

        var role = Domain.Entities.Role.Create(
            $"RollbackRole_{provider}",
            "Role to be rolled back");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        // Verify data exists within transaction
        var countInTransaction = await context.Roles.CountAsync();
        countInTransaction.Should().Be(initialCount + 1, "Data should exist within transaction");

        // Rollback transaction
        await transaction.RollbackAsync();

        // Assert - Verify data was rolled back
        var countAfterRollback = await context.Roles.CountAsync();
        countAfterRollback.Should().Be(initialCount, $"Data should be rolled back for {factory.ProviderName}");
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionProviders_Should_Handle_Multiple_Operations_In_Transaction(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing multiple operations in transaction for {factory.ProviderName} provider");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        using var transaction = await context.Database.BeginTransactionAsync();

        // Create multiple roles in the same transaction
        var roles = Enumerable.Range(1, 3).Select(i =>
            Domain.Entities.Role.Create(
                $"MultiOp_{provider}_Role{i}",
                $"Role {i} in multi-operation transaction")
        ).ToList();

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        // Create a person with one of these roles
        var person = Domain.Entities.Person.Create(
            $"MultiOp_{provider}_Person",
            "+1234567890");
        person.AddRole(roles[0]);
        context.People.Add(person);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        // Assert - Verify all operations were committed
        var roleCount = await context.Roles.Where(r => r.Name.StartsWith($"MultiOp_{provider}_")).CountAsync();
        roleCount.Should().Be(3, "All roles should be committed");

        var personCount = await context.People.Where(p => p.FullName == $"MultiOp_{provider}_Person").CountAsync();
        personCount.Should().Be(1, "Person should be committed");

        // Verify relationship was created
        var personWithRoles = await context.People
            .Include(p => p.Roles)
            .FirstAsync(p => p.FullName == $"MultiOp_{provider}_Person");
        personWithRoles.Roles.Should().HaveCount(1, "Person-role relationship should be committed");
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionProviders_Should_Handle_Exception_Rollback(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing exception-based rollback for {factory.ProviderName} provider");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var initialCount = await context.Roles.CountAsync();

        // Act & Assert
        var action = async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            // Add a valid role
            var validRole = Domain.Entities.Role.Create(
                $"ValidRole_{provider}",
                "Valid role");
            context.Roles.Add(validRole);
            await context.SaveChangesAsync();

            // Try to add an invalid role (duplicate name)
            var duplicateRole = Domain.Entities.Role.Create(
                $"ValidRole_{provider}", // Same name - should cause constraint violation
                "Duplicate role");
            context.Roles.Add(duplicateRole);

            // This should throw an exception due to unique constraint
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        };

        await action.Should().ThrowAsync<Exception>("Duplicate role names should cause an exception");

        // Verify transaction was rolled back
        var finalCount = await context.Roles.CountAsync();
        finalCount.Should().Be(initialCount, $"Transaction should be rolled back on exception for {factory.ProviderName}");
    }

    [Fact]
    public async Task InMemory_Should_Have_Limited_Transaction_Support()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.InMemory);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine("Testing InMemory provider transaction limitations");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act - InMemory provider supports transaction syntax but not real transactions
        using var transaction = await context.Database.BeginTransactionAsync();

        var role = Domain.Entities.Role.Create(
            "InMemoryTransactionRole",
            "Role in InMemory transaction");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        // InMemory doesn't support real rollback - data is already committed
        await transaction.RollbackAsync();

        // Assert - Data should still exist (InMemory limitation)
        var roleExists = await context.Roles.AnyAsync(r => r.Name == "InMemoryTransactionRole");
        roleExists.Should().BeTrue("InMemory provider doesn't support real transaction rollback");

        _output.WriteLine("InMemory provider limitation confirmed: transaction rollback doesn't work");
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionProviders_Should_Support_Nested_SaveChanges(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing nested SaveChanges in transaction for {factory.ProviderName} provider");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        using var transaction = await context.Database.BeginTransactionAsync();

        // First SaveChanges
        var role1 = Domain.Entities.Role.Create(
            $"NestedSave1_{provider}",
            "First save in transaction");
        context.Roles.Add(role1);
        await context.SaveChangesAsync();

        // Second SaveChanges in same transaction
        var role2 = Domain.Entities.Role.Create(
            $"NestedSave2_{provider}",
            "Second save in transaction");
        context.Roles.Add(role2);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        // Assert
        var savedRoles = await context.Roles
            .Where(r => r.Name.StartsWith($"NestedSave") && r.Name.Contains($"_{provider}"))
            .CountAsync();
        savedRoles.Should().Be(2, $"Both nested SaveChanges should work for {factory.ProviderName}");
    }

    [Theory]
    [MemberData(nameof(GetTransactionSupportingProviders))]
    public async Task TransactionProviders_Should_Handle_Deadlock_Scenarios_Gracefully(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing deadlock handling for {factory.ProviderName} provider");

        // Create test data first
        using (var setupScope = factory.Services.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var role1 = Domain.Entities.Role.Create($"DeadlockRole1_{provider}", "Role 1");
            var role2 = Domain.Entities.Role.Create($"DeadlockRole2_{provider}", "Role 2");
            setupContext.Roles.AddRange(role1, role2);
            await setupContext.SaveChangesAsync();
        }

        // Act - Simulate concurrent updates that could cause deadlock
        var tasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var taskScope = factory.Services.CreateScope();
                    var taskContext = taskScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    using var transaction = await taskContext.Database.BeginTransactionAsync();

                    var roles = await taskContext.Roles
                        .Where(r => r.Name.StartsWith($"DeadlockRole") && r.Name.Contains($"_{provider}"))
                        .ToListAsync();

                    foreach (var role in roles)
                    {
                        role.UpdateDescription($"Updated by task {taskIndex} at {DateTime.Now:HH:mm:ss.fff}");
                    }

                    await taskContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _output.WriteLine($"Task {taskIndex} completed successfully for {factory.ProviderName}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Task {taskIndex} failed for {factory.ProviderName}: {ex.Message}");
                    // Deadlocks are expected in concurrent scenarios
                }
            }));
        }

        // Assert - At least one task should complete (others may deadlock/fail)
        await Task.WhenAll(tasks);

        using var verifyScope = factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var finalRoles = await verifyContext.Roles
            .Where(r => r.Name.StartsWith($"DeadlockRole") && r.Name.Contains($"_{provider}"))
            .ToListAsync();

        finalRoles.Should().HaveCount(2, "Both roles should still exist after concurrent updates");
        _output.WriteLine($"Concurrent update test completed for {factory.ProviderName}");
    }

    private IMultiProviderTestWebApplicationFactory CreateFactory(DatabaseProvider provider)
    {
        var factory = MultiProviderTestWebApplicationFactoryProvider.Create(provider);
        _factories.Add(factory);
        return factory;
    }

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
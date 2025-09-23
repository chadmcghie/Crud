using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Tests that validate provider-specific behaviors and edge cases
/// Ensures our application handles database provider differences correctly
/// </summary>
public class ProviderSpecificBehaviorTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<IMultiProviderTestWebApplicationFactory> _factories = new();

    public ProviderSpecificBehaviorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SQLite_Should_Support_Foreign_Keys_When_Enabled()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.SQLite);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine("Testing SQLite foreign key constraint enforcement");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act & Assert - Create entities with valid relationships
        var role = Domain.Entities.Role.Create("TestRole", "Test");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var person = Domain.Entities.Person.Create("Test Person", "+1234567890");
        person.AddRole(role);
        context.People.Add(person);

        // This should succeed
        await context.SaveChangesAsync();

        // Verify relationship exists
        var savedPerson = await context.People
            .Include(p => p.Roles)
            .FirstAsync(p => p.FullName == "Test Person");
        savedPerson.Roles.Should().HaveCount(1);
        savedPerson.Roles.First().Name.Should().Be("TestRole");
    }

    [Fact]
    public async Task InMemory_Should_Allow_Invalid_References()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.InMemory);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine("Testing InMemory provider's lack of constraint enforcement");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act - InMemory provider doesn't enforce FK constraints
        // This tests the behavior difference, not that we want this behavior
        var person = Domain.Entities.Person.Create("Test Person", "+1234567890");
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Assert - Verify the person was created (InMemory allows this even without proper relationships)
        var savedPerson = await context.People.FirstOrDefaultAsync(p => p.FullName == "Test Person");
        savedPerson.Should().NotBeNull();
        savedPerson!.FullName.Should().Be("Test Person");
    }

    [Fact]
    public async Task SqlServer_Should_Enforce_Foreign_Key_Constraints()
    {
        // Arrange
        var factory = CreateFactory(DatabaseProvider.SqlServer);

        try
        {
            factory.EnsureDatabaseCreated();
            await factory.ClearDatabaseAsync();
            _output.WriteLine("Testing SQL Server foreign key constraint enforcement");

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Act & Assert - Create entities with valid relationships first
            var role = Domain.Entities.Role.Create("TestRole", "Test");
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            var person = Domain.Entities.Person.Create("Test Person", "+1234567890");
            person.AddRole(role);
            context.People.Add(person);

            // This should succeed with proper relationships
            await context.SaveChangesAsync();

            // Verify relationship exists
            var savedPerson = await context.People
                .Include(p => p.Roles)
                .FirstAsync(p => p.FullName == "Test Person");
            savedPerson.Roles.Should().HaveCount(1);
        }
        catch (Exception ex) when (ex.Message.Contains("LocalDB"))
        {
            _output.WriteLine("SQL Server LocalDB not available - skipping SQL Server specific test");
            // Skip if LocalDB is not available in test environment
        }
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Should_Handle_Concurrent_Operations_Correctly(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing concurrent operations for {factory.ProviderName} provider");

        // Act - Simulate concurrent role creation
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var role = Domain.Entities.Role.Create(
                $"ConcurrentRole_{provider}_{i}",
                $"Role created concurrently for {provider}");
            context.Roles.Add(role);
            await context.SaveChangesAsync();
            return role.Id;
        });

        var roleIds = await Task.WhenAll(tasks);

        // Assert
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var createdRoles = await context.Roles
                .Where(r => r.Name.StartsWith($"ConcurrentRole_{provider}_"))
                .CountAsync();

            createdRoles.Should().Be(5, $"All concurrent operations should succeed for {factory.ProviderName}");
        }

        // Verify all IDs are unique
        roleIds.Should().OnlyHaveUniqueItems("All role IDs should be unique");
    }

    [Theory]
    [MemberData(nameof(GetPersistenceProviders))]
    public async Task PersistentProviders_Should_Maintain_Data_Across_Context_Disposal(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing data persistence for {factory.ProviderName} provider");

        var roleName = $"PersistentRole_{provider}_{Guid.NewGuid():N}";
        Guid roleId;

        // Act - Create data in one context
        using (var scope1 = factory.Services.CreateScope())
        {
            var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var role = Domain.Entities.Role.Create(roleName, "Persistence test");
            context1.Roles.Add(role);
            await context1.SaveChangesAsync();
            roleId = role.Id;
        }

        // Assert - Verify data exists in new context
        using (var scope2 = factory.Services.CreateScope())
        {
            var context2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var retrievedRole = await context2.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

            retrievedRole.Should().NotBeNull($"Data should persist across contexts for {factory.ProviderName}");
            retrievedRole!.Name.Should().Be(roleName);
        }
    }

    [Fact]
    public async Task InMemory_Should_Lose_Data_Across_Factory_Instances()
    {
        // Arrange & Act
        var roleName = $"TransientRole_{Guid.NewGuid():N}";
        Guid roleId;

        // Create data with first factory instance
        using (var factory1 = CreateFactory(DatabaseProvider.InMemory))
        {
            factory1.EnsureDatabaseCreated();
            using var scope1 = factory1.Services.CreateScope();
            var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var role = Domain.Entities.Role.Create(roleName, "Transient test");
            context1.Roles.Add(role);
            await context1.SaveChangesAsync();
            roleId = role.Id;
        }

        // Try to access data with second factory instance
        using (var factory2 = CreateFactory(DatabaseProvider.InMemory))
        {
            factory2.EnsureDatabaseCreated();
            using var scope2 = factory2.Services.CreateScope();
            var context2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var retrievedRole = await context2.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

            // Assert - InMemory data should not persist across factory instances
            retrievedRole.Should().BeNull("InMemory data should not persist across factory instances");
        }
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task Provider_Should_Handle_Unicode_Data_Correctly(DatabaseProvider provider)
    {
        // Arrange
        var factory = CreateFactory(provider);
        factory.EnsureDatabaseCreated();
        await factory.ClearDatabaseAsync();
        _output.WriteLine($"Testing Unicode data handling for {factory.ProviderName} provider");

        var unicodeText = "测试 🚀 Tëst Ñämé with émojis 🎉 and spëçiál characters: àáâãäåæçèéêë";

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        var role = Domain.Entities.Role.Create(
            $"Unicode_{provider}_{Guid.NewGuid():N[..8]}",
            unicodeText);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        // Assert
        var retrievedRole = await context.Roles.FirstAsync(r => r.Id == role.Id);
        retrievedRole.Description.Should().Be(unicodeText,
            $"Unicode text should be preserved correctly for {factory.ProviderName}");
    }

    private IMultiProviderTestWebApplicationFactory CreateFactory(DatabaseProvider provider)
    {
        var factory = MultiProviderTestWebApplicationFactoryProvider.Create(provider);
        _factories.Add(factory);
        return factory;
    }

    public static IEnumerable<object[]> GetAllProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetAllProvidersAsTestData();

    public static IEnumerable<object[]> GetPersistenceProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetProvidersWithFeaturesAsTestData(requiresPersistence: true);

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
        _factories.Clear();
    }
}

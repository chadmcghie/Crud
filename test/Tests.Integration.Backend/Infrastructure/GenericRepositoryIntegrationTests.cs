using System;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Specifications;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Integration tests for the generic repository pattern using real database and DI container
/// Tests the full integration between Ardalis.Specification and Entity Framework Core
/// </summary>
public class GenericRepositoryIntegrationTests : IntegrationTestBase
{
    public GenericRepositoryIntegrationTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public Task IRepository_ShouldBeRegisteredInDIContainer()
    {
        return RunWithCleanDatabaseAsync(() =>
        {
            // Arrange & Act
            var personRepository = Scope.ServiceProvider.GetService<IRepository<Person>>();
            var roleRepository = Scope.ServiceProvider.GetService<IRepository<Role>>();

            // Assert
            Assert.NotNull(personRepository);
            Assert.NotNull(roleRepository);

            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task GenericRepository_ShouldWorkWithSpecifications_EndToEnd()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var personRepository = Scope.ServiceProvider.GetRequiredService<IRepository<Person>>();
            var roleRepository = Scope.ServiceProvider.GetRequiredService<IRepository<Role>>();

            // Create test data
            var adminRole = Role.Create("Administrator", "Full system access");
            var userRole = Role.Create("User", "Limited access");

            await roleRepository.AddAsync(adminRole);
            await roleRepository.AddAsync(userRole);
            await DbContext.SaveChangesAsync();

            var adminPerson = Person.Create("Admin User", "123-456-7890");
            adminPerson.AddRole(adminRole);

            var regularPerson = Person.Create("Regular User", "098-765-4321");
            regularPerson.AddRole(userRole);

            var noRolePerson = Person.Create("No Role User", "555-666-7777");

            await personRepository.AddAsync(adminPerson);
            await personRepository.AddAsync(regularPerson);
            await personRepository.AddAsync(noRolePerson);
            await DbContext.SaveChangesAsync();

            // Act & Assert - Test PersonsByRoleSpec
            var admins = await personRepository.ListAsync(new PersonsByRoleSpec("Administrator"));
            Assert.Single(admins);
            Assert.Contains(admins, p => p.FullName == "Admin User");
            Assert.Single(admins.First().Roles);
            Assert.Contains(admins.First().Roles, r => r.Name == "Administrator");

            // Act & Assert - Test PersonByNameSpec
            var usersByName = await personRepository.ListAsync(new PersonByNameSpec("admin"));
            Assert.Single(usersByName);
            Assert.Contains(usersByName, p => p.FullName == "Admin User");

            // Act & Assert - Test PersonByIdWithRolesSpec
            var personById = await personRepository.FirstOrDefaultAsync(new PersonByIdWithRolesSpec(regularPerson.Id));
            Assert.NotNull(personById);
            Assert.Equal("Regular User", personById!.FullName);
            Assert.Single(personById.Roles);
            Assert.Contains(personById.Roles, r => r.Name == "User");

            // Act & Assert - Test basic repository methods
            var allPersons = await personRepository.ListAsync();
            Assert.Equal(3, allPersons.Count);

            var personCount = await personRepository.CountAsync();
            Assert.Equal(3, personCount);

            var adminExists = await personRepository.AnyAsync(new PersonsByRoleSpec("Administrator"));
            Assert.True(adminExists);

            var managerExists = await personRepository.AnyAsync(new PersonsByRoleSpec("Manager"));
            Assert.False(managerExists);
        });
    }

    [Fact]
    public async Task GenericRepository_ShouldWorkAlongsideCustomRepositories()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var genericPersonRepository = Scope.ServiceProvider.GetRequiredService<IRepository<Person>>();
            var customPersonRepository = Scope.ServiceProvider.GetRequiredService<App.Abstractions.IPersonRepository>();

            // Act - Add via generic repository
            var person = Person.Create("Test Person", "111-222-3333");
            await genericPersonRepository.AddAsync(person);
            await DbContext.SaveChangesAsync();

            // Act - Retrieve via custom repository
            var retrievedPerson = await customPersonRepository.GetAsync(person.Id);

            // Assert
            Assert.NotNull(retrievedPerson);
            Assert.Equal("Test Person", retrievedPerson!.FullName);
            Assert.Equal("111-222-3333", retrievedPerson.Phone);

            // Act - Update via custom repository
            retrievedPerson.UpdatePhone("999-888-7777");
            await customPersonRepository.UpdateAsync(retrievedPerson);

            // Act - Retrieve via generic repository
            var updatedPerson = await genericPersonRepository.GetByIdAsync(person.Id);

            // Assert
            Assert.NotNull(updatedPerson);
            Assert.Equal("999-888-7777", updatedPerson!.Phone);
        });
    }
}

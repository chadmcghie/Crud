using System;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Specifications;
using FluentAssertions;
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
    public async Task IRepository_ShouldBeRegisteredInDIContainer()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange & Act
            var personRepository = Scope.ServiceProvider.GetService<IRepository<Person>>();
            var roleRepository = Scope.ServiceProvider.GetService<IRepository<Role>>();

            // Assert
            personRepository.Should().NotBeNull("IRepository<Person> should be registered in DI container");
            roleRepository.Should().NotBeNull("IRepository<Role> should be registered in DI container");
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
            var adminRole = new Role { Name = "Administrator", Description = "Full system access" };
            var userRole = new Role { Name = "User", Description = "Limited access" };

            await roleRepository.AddAsync(adminRole);
            await roleRepository.AddAsync(userRole);
            await DbContext.SaveChangesAsync();

            var adminPerson = new Person { FullName = "Admin User", Phone = "123-456-7890" };
            adminPerson.Roles.Add(adminRole);

            var regularPerson = new Person { FullName = "Regular User", Phone = "098-765-4321" };
            regularPerson.Roles.Add(userRole);

            var noRolePerson = new Person { FullName = "No Role User", Phone = "555-666-7777" };

            await personRepository.AddAsync(adminPerson);
            await personRepository.AddAsync(regularPerson);
            await personRepository.AddAsync(noRolePerson);
            await DbContext.SaveChangesAsync();

            // Act & Assert - Test PersonsByRoleSpec
            var admins = await personRepository.ListAsync(new PersonsByRoleSpec("Administrator"));
            admins.Should().HaveCount(1);
            admins.Should().Contain(p => p.FullName == "Admin User");
            admins.First().Roles.Should().HaveCount(1);
            admins.First().Roles.Should().Contain(r => r.Name == "Administrator");

            // Act & Assert - Test PersonByNameSpec
            var usersByName = await personRepository.ListAsync(new PersonByNameSpec("admin"));
            usersByName.Should().HaveCount(1);
            usersByName.Should().Contain(p => p.FullName == "Admin User");

            // Act & Assert - Test PersonByIdWithRolesSpec
            var personById = await personRepository.FirstOrDefaultAsync(new PersonByIdWithRolesSpec(regularPerson.Id));
            personById.Should().NotBeNull();
            personById!.FullName.Should().Be("Regular User");
            personById.Roles.Should().HaveCount(1);
            personById.Roles.Should().Contain(r => r.Name == "User");

            // Act & Assert - Test basic repository methods
            var allPersons = await personRepository.ListAsync();
            allPersons.Should().HaveCount(3);

            var personCount = await personRepository.CountAsync();
            personCount.Should().Be(3);

            var adminExists = await personRepository.AnyAsync(new PersonsByRoleSpec("Administrator"));
            adminExists.Should().BeTrue();

            var managerExists = await personRepository.AnyAsync(new PersonsByRoleSpec("Manager"));
            managerExists.Should().BeFalse();
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
            var person = new Person { FullName = "Test Person", Phone = "111-222-3333" };
            await genericPersonRepository.AddAsync(person);
            await DbContext.SaveChangesAsync();

            // Act - Retrieve via custom repository
            var retrievedPerson = await customPersonRepository.GetAsync(person.Id);

            // Assert
            retrievedPerson.Should().NotBeNull();
            retrievedPerson!.FullName.Should().Be("Test Person");
            retrievedPerson.Phone.Should().Be("111-222-3333");

            // Act - Update via custom repository
            retrievedPerson.Phone = "999-888-7777";
            await customPersonRepository.UpdateAsync(retrievedPerson);

            // Act - Retrieve via generic repository
            var updatedPerson = await genericPersonRepository.GetByIdAsync(person.Id);

            // Assert
            updatedPerson.Should().NotBeNull();
            updatedPerson!.Phone.Should().Be("999-888-7777");
        });
    }
}

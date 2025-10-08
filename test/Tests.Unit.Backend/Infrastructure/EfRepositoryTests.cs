using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Specifications;
using Infrastructure.Data;
using Infrastructure.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure;

/// <summary>
/// Unit tests for the generic repository implementation using Ardalis.Specification
/// </summary>
public class EfRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Role> _roleRepository;

    public EfRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _personRepository = new EfRepository<Person>(_context);
        _roleRepository = new EfRepository<Role>(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddPersonSuccessfully()
    {
        // Arrange
        var person = Person.Create("John Doe", "123-456-7890");

        // Act
        var result = await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("John Doe", result.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPersonById()
    {
        // Arrange
        var person = Person.Create("Jane Doe", "987-654-3210");
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        var result = await _personRepository.GetByIdAsync(person.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result.FullName);
        Assert.Equal("987-654-3210", result.Phone);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpecification_ShouldReturnMatchingPerson()
    {
        // Arrange
        var role = Role.Create("Admin", "Administrator");
        await _roleRepository.AddAsync(role);

        var person = Person.Create("Admin User", "111-222-3333");
        person.AddRole(role);
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        var specification = new PersonsByRoleSpec("Admin");

        // Act
        var result = await _personRepository.FirstOrDefaultAsync(specification);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Admin User", result.FullName);
        Assert.Single(result.Roles);
        Assert.Equal("Admin", result.Roles.First().Name);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithPersonByNameSpec_ShouldReturnMatchingPerson()
    {
        // Arrange
        var person = Person.Create("Test User", "444-555-6666");
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        var specification = new PersonByNameSpec("test");

        // Act
        var result = await _personRepository.FirstOrDefaultAsync(specification);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test User", result.FullName);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithPersonByIdWithRolesSpec_ShouldIncludeRoles()
    {
        // Arrange
        var role = Role.Create("User", "Regular User");
        await _roleRepository.AddAsync(role);

        var person = Person.Create("User With Role", "777-888-9999");
        person.AddRole(role);
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        var specification = new PersonByIdWithRolesSpec(person.Id);

        // Act
        var result = await _personRepository.FirstOrDefaultAsync(specification);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("User With Role", result.FullName);
        Assert.Single(result.Roles);
        Assert.Equal("User", result.Roles.First().Name);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnAllPersons()
    {
        // Arrange
        var person1 = Person.Create("Person One", "111-111-1111");
        var person2 = Person.Create("Person Two", "222-222-2222");

        await _personRepository.AddAsync(person1);
        await _personRepository.AddAsync(person2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _personRepository.ListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.FullName == "Person One");
        Assert.Contains(result, p => p.FullName == "Person Two");
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var person1 = Person.Create("Person A", "000-111-2222");
        var person2 = Person.Create("Person B", "000-333-4444");

        await _personRepository.AddAsync(person1);
        await _personRepository.AddAsync(person2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _personRepository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

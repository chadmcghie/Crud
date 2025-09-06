using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Specifications;
using FluentAssertions;
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
            .Options;

        _context = new ApplicationDbContext(options);
        _personRepository = new EfRepository<Person>(_context);
        _roleRepository = new EfRepository<Role>(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddPersonSuccessfully()
    {
        // Arrange
        var person = new Person { FullName = "John Doe", Phone = "123-456-7890" };

        // Act
        var result = await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPersonById()
    {
        // Arrange
        var person = new Person { FullName = "Jane Doe", Phone = "987-654-3210" };
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        var result = await _personRepository.GetByIdAsync(person.Id);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("Jane Doe");
        result.Phone.Should().Be("987-654-3210");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpecification_ShouldReturnMatchingPerson()
    {
        // Arrange
        var role = new Role { Name = "Admin", Description = "Administrator" };
        await _roleRepository.AddAsync(role);

        var person = new Person { FullName = "Admin User", Phone = "111-222-3333" };
        person.Roles.Add(role);
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        var specification = new PersonsByRoleSpec("Admin");

        // Act
        var result = await _personRepository.FirstOrDefaultAsync(specification);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("Admin User");
        result.Roles.Should().HaveCount(1);
        result.Roles.First().Name.Should().Be("Admin");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithPersonByNameSpec_ShouldReturnMatchingPerson()
    {
        // Arrange
        var person = new Person { FullName = "Test User", Phone = "444-555-6666" };
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        var specification = new PersonByNameSpec("test");

        // Act
        var result = await _personRepository.FirstOrDefaultAsync(specification);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithPersonByIdWithRolesSpec_ShouldIncludeRoles()
    {
        // Arrange
        var role = new Role { Name = "User", Description = "Regular User" };
        await _roleRepository.AddAsync(role);

        var person = new Person { FullName = "User With Role", Phone = "777-888-9999" };
        person.Roles.Add(role);
        await _personRepository.AddAsync(person);
        await _context.SaveChangesAsync();

        var specification = new PersonByIdWithRolesSpec(person.Id);

        // Act
        var result = await _personRepository.FirstOrDefaultAsync(specification);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("User With Role");
        result.Roles.Should().HaveCount(1);
        result.Roles.First().Name.Should().Be("User");
    }

    [Fact]
    public async Task ListAsync_ShouldReturnAllPersons()
    {
        // Arrange
        var person1 = new Person { FullName = "Person One", Phone = "111-111-1111" };
        var person2 = new Person { FullName = "Person Two", Phone = "222-222-2222" };

        await _personRepository.AddAsync(person1);
        await _personRepository.AddAsync(person2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _personRepository.ListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.FullName == "Person One");
        result.Should().Contain(p => p.FullName == "Person Two");
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var person1 = new Person { FullName = "Person A", Phone = "000-111-2222" };
        var person2 = new Person { FullName = "Person B", Phone = "000-333-4444" };

        await _personRepository.AddAsync(person1);
        await _personRepository.AddAsync(person2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _personRepository.CountAsync();

        // Assert
        count.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

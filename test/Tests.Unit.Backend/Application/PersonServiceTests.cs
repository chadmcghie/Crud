using App.Abstractions;
using App.Services;
using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Application;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _mockPersonRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly PersonService _personService;

    public PersonServiceTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _personService = new PersonService(_mockPersonRepository.Object, _mockRoleRepository.Object);
    }

    public class GetAsync : PersonServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldReturnPerson()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var expectedPerson = PersonTestDataBuilder.Default().Build();
            expectedPerson.Id = personId;

            _mockPersonRepository.Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPerson);

            // Act
            var result = await _personService.GetAsync(personId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedPerson);
            _mockPersonRepository.Verify(x => x.GetAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var personId = Guid.NewGuid();
            _mockPersonRepository.Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Person?)null);

            // Act
            var result = await _personService.GetAsync(personId);

            // Assert
            result.Should().BeNull();
            _mockPersonRepository.Verify(x => x.GetAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPassCancellationToken()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            // Act
            await _personService.GetAsync(personId, cancellationToken);

            // Assert
            _mockPersonRepository.Verify(x => x.GetAsync(personId, cancellationToken), Times.Once);
        }
    }

    public class ListAsync : PersonServiceTests
    {
        [Fact]
        public async Task ShouldReturnAllPersons()
        {
            // Arrange
            var persons = new List<Person>
            {
                PersonTestDataBuilder.Default().WithFullName("John Doe").Build(),
                PersonTestDataBuilder.Default().WithFullName("Jane Smith").Build()
            };

            _mockPersonRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(persons);

            // Act
            var result = await _personService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(persons);
            _mockPersonRepository.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithEmptyRepository_ShouldReturnEmptyList()
        {
            // Arrange
            _mockPersonRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Person>());

            // Act
            var result = await _personService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    public class CreateAsync : PersonServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldCreatePerson()
        {
            // Arrange
            var fullName = "John Doe";
            var phone = "555-1234";
            var expectedPerson = PersonTestDataBuilder.Default()
                .WithFullName(fullName)
                .WithPhone(phone)
                .Build();

            _mockPersonRepository.Setup(x => x.AddAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPerson);

            // Act
            var result = await _personService.CreateAsync(fullName, phone);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be(fullName);
            result.Phone.Should().Be(phone);
            _mockPersonRepository.Verify(x => x.AddAsync(It.Is<Person>(p => 
                p.FullName == fullName && p.Phone == phone), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithRoles_ShouldCreatePersonWithRoles()
        {
            // Arrange
            var fullName = "John Doe";
            var phone = "555-1234";
            var role1 = RoleTestDataBuilder.Default().WithName("Admin").Build();
            var role2 = RoleTestDataBuilder.Default().WithName("User").Build();
            var roleIds = new[] { role1.Id, role2.Id };

            _mockRoleRepository.Setup(x => x.GetAsync(role1.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(role1);
            _mockRoleRepository.Setup(x => x.GetAsync(role2.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(role2);

            var expectedPerson = PersonTestDataBuilder.Default()
                .WithFullName(fullName)
                .WithPhone(phone)
                .WithRoles(role1, role2)
                .Build();

            _mockPersonRepository.Setup(x => x.AddAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPerson);

            // Act
            var result = await _personService.CreateAsync(fullName, phone, roleIds);

            // Assert
            result.Should().NotBeNull();
            result.Roles.Should().HaveCount(2);
            result.Roles.Should().Contain(role1);
            result.Roles.Should().Contain(role2);
        }

        [Fact]
        public async Task WithInvalidRoleId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var fullName = "John Doe";
            var invalidRoleId = Guid.NewGuid();
            var roleIds = new[] { invalidRoleId };

            _mockRoleRepository.Setup(x => x.GetAsync(invalidRoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Role?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _personService.CreateAsync(fullName, null, roleIds));

            exception.Message.Should().Contain($"Role {invalidRoleId} not found");
        }
    }

    public class UpdateAsync : PersonServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldUpdatePerson()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var existingPerson = PersonTestDataBuilder.Default().Build();
            existingPerson.Id = personId;

            var newFullName = "Updated Name";
            var newPhone = "555-9999";

            _mockPersonRepository.Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPerson);

            // Act
            await _personService.UpdateAsync(personId, newFullName, newPhone);

            // Assert
            existingPerson.FullName.Should().Be(newFullName);
            existingPerson.Phone.Should().Be(newPhone);
            _mockPersonRepository.Verify(x => x.UpdateAsync(existingPerson, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidPersonId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidPersonId = Guid.NewGuid();
            _mockPersonRepository.Setup(x => x.GetAsync(invalidPersonId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Person?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _personService.UpdateAsync(invalidPersonId, "Name", "Phone"));

            exception.Message.Should().Contain($"Person {invalidPersonId} not found");
        }

        [Fact]
        public async Task WithRoles_ShouldUpdatePersonRoles()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var existingPerson = PersonTestDataBuilder.Default().Build();
            existingPerson.Id = personId;

            var newRole = RoleTestDataBuilder.Default().WithName("NewRole").Build();
            var roleIds = new[] { newRole.Id };

            _mockPersonRepository.Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPerson);
            _mockRoleRepository.Setup(x => x.GetAsync(newRole.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newRole);

            // Act
            await _personService.UpdateAsync(personId, "Name", "Phone", roleIds);

            // Assert
            existingPerson.Roles.Should().HaveCount(1);
            existingPerson.Roles.Should().Contain(newRole);
        }
    }

    public class DeleteAsync : PersonServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldDeletePerson()
        {
            // Arrange
            var personId = Guid.NewGuid();

            // Act
            await _personService.DeleteAsync(personId);

            // Assert
            _mockPersonRepository.Verify(x => x.DeleteAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPassCancellationToken()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            // Act
            await _personService.DeleteAsync(personId, cancellationToken);

            // Assert
            _mockPersonRepository.Verify(x => x.DeleteAsync(personId, cancellationToken), Times.Once);
        }
    }
}

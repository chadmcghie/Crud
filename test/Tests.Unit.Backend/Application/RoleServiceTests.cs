using App.Abstractions;
using App.Services;
using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Application;

public class RoleServiceTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly RoleService _roleService;

    public RoleServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _roleService = new RoleService(_mockRoleRepository.Object);
    }

    public class GetAsync : RoleServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldReturnRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var expectedRole = RoleTestDataBuilder.Default().Build();
            expectedRole.Id = roleId;

            _mockRoleRepository.Setup(x => x.GetAsync(roleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.GetAsync(roleId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedRole);
            _mockRoleRepository.Verify(x => x.GetAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _mockRoleRepository.Setup(x => x.GetAsync(roleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.GetAsync(roleId);

            // Assert
            result.Should().BeNull();
        }
    }

    public class ListAsync : RoleServiceTests
    {
        [Fact]
        public async Task ShouldReturnAllRoles()
        {
            // Arrange
            var roles = new List<Role>
            {
                RoleTestDataBuilder.Default().WithName("Admin").Build(),
                RoleTestDataBuilder.Default().WithName("User").Build()
            };

            _mockRoleRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(roles);

            // Act
            var result = await _roleService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(roles);
        }

        [Fact]
        public async Task WithEmptyRepository_ShouldReturnEmptyList()
        {
            // Arrange
            _mockRoleRepository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Role>());

            // Act
            var result = await _roleService.ListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }

    public class CreateAsync : RoleServiceTests
    {
        [Fact]
        public async Task WithNewRoleName_ShouldCreateRole()
        {
            // Arrange
            var roleName = "NewRole";
            var description = "New role description";
            var expectedRole = RoleTestDataBuilder.Default()
                .WithName(roleName)
                .WithDescription(description)
                .Build();

            _mockRoleRepository.Setup(x => x.GetByNameAsync(roleName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Role?)null);
            _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.CreateAsync(roleName, description);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(roleName);
            result.Description.Should().Be(description);
            _mockRoleRepository.Verify(x => x.GetByNameAsync(roleName, It.IsAny<CancellationToken>()), Times.Once);
            _mockRoleRepository.Verify(x => x.AddAsync(It.Is<Role>(r =>
                r.Name == roleName && r.Description == description), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithExistingRoleName_ShouldReturnExistingRole()
        {
            // Arrange
            var roleName = "ExistingRole";
            var existingRole = RoleTestDataBuilder.Default().WithName(roleName).Build();

            _mockRoleRepository.Setup(x => x.GetByNameAsync(roleName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRole);

            // Act
            var result = await _roleService.CreateAsync(roleName, "Some description");

            // Assert
            result.Should().Be(existingRole);
            _mockRoleRepository.Verify(x => x.GetByNameAsync(roleName, It.IsAny<CancellationToken>()), Times.Once);
            _mockRoleRepository.Verify(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithoutDescription_ShouldCreateRoleWithNullDescription()
        {
            // Arrange
            var roleName = "SimpleRole";
            var expectedRole = RoleTestDataBuilder.Default()
                .WithName(roleName)
                .WithDescription(null)
                .Build();

            _mockRoleRepository.Setup(x => x.GetByNameAsync(roleName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Role?)null);
            _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _roleService.CreateAsync(roleName);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(roleName);
            result.Description.Should().BeNull();
        }
    }

    public class UpdateAsync : RoleServiceTests
    {
        [Fact]
        public async Task WithValidData_ShouldUpdateRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var existingRole = RoleTestDataBuilder.Default().Build();
            existingRole.Id = roleId;

            var newName = "UpdatedRole";
            var newDescription = "Updated description";

            _mockRoleRepository.Setup(x => x.GetAsync(roleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRole);

            // Act
            await _roleService.UpdateAsync(roleId, newName, newDescription);

            // Assert
            existingRole.Name.Should().Be(newName);
            existingRole.Description.Should().Be(newDescription);
            _mockRoleRepository.Verify(x => x.UpdateAsync(existingRole, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithInvalidRoleId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidRoleId = Guid.NewGuid();
            _mockRoleRepository.Setup(x => x.GetAsync(invalidRoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Role?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _roleService.UpdateAsync(invalidRoleId, "Name", "Description"));

            exception.Message.Should().Contain($"Role {invalidRoleId} not found");
        }
    }

    public class DeleteAsync : RoleServiceTests
    {
        [Fact]
        public async Task WithValidId_ShouldDeleteRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();

            // Act
            await _roleService.DeleteAsync(roleId);

            // Assert
            _mockRoleRepository.Verify(x => x.DeleteAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPassCancellationToken()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            // Act
            await _roleService.DeleteAsync(roleId, cancellationToken);

            // Assert
            _mockRoleRepository.Verify(x => x.DeleteAsync(roleId, cancellationToken), Times.Once);
        }
    }
}


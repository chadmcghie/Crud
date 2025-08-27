using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Domain;

public class RoleTests
{
    public class Construction : RoleTests
    {
        [Fact]
        public void WithValidData_ShouldCreateRole()
        {
            // Arrange & Act
            var role = RoleTestDataBuilder.Default()
                .WithName("Administrator")
                .WithDescription("System administrator role")
                .Build();

            // Assert
            role.Should().NotBeNull();
            role.Id.Should().NotBeEmpty();
            role.Name.Should().Be("Administrator");
            role.Description.Should().Be("System administrator role");
        }

        [Fact]
        public void WithoutDescription_ShouldCreateRoleWithNullDescription()
        {
            // Arrange & Act
            var role = RoleTestDataBuilder.Default()
                .WithName("User")
                .WithDescription(null)
                .Build();

            // Assert
            role.Should().NotBeNull();
            role.Name.Should().Be("User");
            role.Description.Should().BeNull();
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var role1 = RoleTestDataBuilder.Default().Build();
            var role2 = RoleTestDataBuilder.Default().Build();

            // Assert
            role1.Id.Should().NotBe(role2.Id);
        }
    }

    public class Validation : RoleTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void WithInvalidName_ShouldStillCreateRole_ValidationHandledByDataAnnotations(string invalidName)
        {
            // Note: Entity creation doesn't enforce validation - that's handled by the framework
            // This test documents current behavior
            
            // Arrange & Act
            var role = RoleTestDataBuilder.Default()
                .WithName(invalidName)
                .Build();

            // Assert
            role.Should().NotBeNull();
            role.Name.Should().Be(invalidName);
        }
    }
}


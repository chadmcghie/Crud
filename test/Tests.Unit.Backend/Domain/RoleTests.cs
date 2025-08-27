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
        public void WithInvalidName_ShouldThrowArgumentException(string invalidName)
        {
            // Domain entities now use GuardClauses for immediate validation
            // This enforces invariants at the domain level
            
            // Arrange & Act & Assert
            var action = () => RoleTestDataBuilder.Default()
                .WithName(invalidName)
                .Build();

            action.Should().Throw<ArgumentException>()
                .WithMessage("Required input*was empty*");
        }

        [Fact]
        public void WithNameTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longName = new string('a', 101); // Max length is 100
            
            // Act & Assert
            var action = () => RoleTestDataBuilder.Default()
                .WithName(longName)
                .Build();

            action.Should().Throw<ArgumentException>()
                .WithMessage("Input*too long*");
        }

        [Fact]
        public void WithDescriptionTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longDescription = new string('a', 501); // Max length is 500
            
            // Act & Assert
            var action = () => RoleTestDataBuilder.Default()
                .WithDescription(longDescription)
                .Build();

            action.Should().Throw<ArgumentException>()
                .WithMessage("Input*too long*");
        }
    }
}


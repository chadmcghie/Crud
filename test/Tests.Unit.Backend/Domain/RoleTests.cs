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
            Assert.NotNull(role);
            Assert.NotEqual(Guid.Empty, role.Id);
            Assert.Equal("Administrator", role.Name);
            Assert.Equal("System administrator role", role.Description);
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
            Assert.NotNull(role);
            Assert.Equal("User", role.Name);
            Assert.Null(role.Description);
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var role1 = RoleTestDataBuilder.Default().Build();
            var role2 = RoleTestDataBuilder.Default().Build();

            // Assert
            Assert.NotEqual(role2.Id, role1.Id);
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

            var ex = Assert.Throws<ArgumentException>(action);
            Assert.Contains("Required input", ex.Message);
            Assert.Contains("was empty", ex.Message);
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

            var ex = Assert.Throws<ArgumentException>(action);
            Assert.Contains("Input", ex.Message);
            Assert.Contains("too long", ex.Message);
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

            var ex = Assert.Throws<ArgumentException>(action);
            Assert.Contains("Input", ex.Message);
            Assert.Contains("too long", ex.Message);
        }
    }
}


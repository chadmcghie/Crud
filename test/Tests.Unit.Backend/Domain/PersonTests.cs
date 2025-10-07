using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Domain;

public class PersonTests
{
    public class Construction : PersonTests
    {
        [Fact]
        public void WithValidData_ShouldCreatePerson()
        {
            // Arrange & Act
            var person = PersonTestDataBuilder.Default()
                .WithFullName("John Doe")
                .WithPhone("555-1234")
                .Build();

            // Assert
            Assert.NotNull(person);
            Assert.NotEqual(Guid.Empty, person.Id);
            Assert.Equal("John Doe", person.FullName);
            Assert.Equal("555-1234", person.Phone);
            Assert.NotNull(person.Roles);
            Assert.Empty(person.Roles);
        }

        [Fact]
        public void WithoutPhone_ShouldCreatePersonWithNullPhone()
        {
            // Arrange & Act
            var person = PersonTestDataBuilder.Default()
                .WithFullName("Jane Doe")
                .WithPhone(null)
                .Build();

            // Assert
            Assert.NotNull(person);
            Assert.Equal("Jane Doe", person.FullName);
            Assert.Null(person.Phone);
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var person1 = PersonTestDataBuilder.Default().Build();
            var person2 = PersonTestDataBuilder.Default().Build();

            // Assert
            Assert.NotEqual(person2.Id, person1.Id);
        }
    }

    public class RoleManagement : PersonTests
    {
        [Fact]
        public void WithRoles_ShouldAssignRoles()
        {
            // Arrange
            var role1 = RoleTestDataBuilder.Default().WithName("Admin").Build();
            var role2 = RoleTestDataBuilder.Default().WithName("User").Build();

            // Act
            var person = PersonTestDataBuilder.Default()
                .WithRoles(role1, role2)
                .Build();

            // Assert
            Assert.Equal(2, person.Roles.Count);
            Assert.Contains(role1, person.Roles);
            Assert.Contains(role2, person.Roles);
        }

        [Fact]
        public void WithoutRoles_ShouldHaveEmptyRoleCollection()
        {
            // Arrange & Act
            var person = PersonTestDataBuilder.Default()
                .WithoutRoles()
                .Build();

            // Assert
            Assert.NotNull(person.Roles);
            Assert.Empty(person.Roles);
        }
    }

    public class Validation : PersonTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void WithInvalidFullName_ShouldThrowArgumentException(string invalidName)
        {
            // Domain entities now use GuardClauses for immediate validation
            // This enforces invariants at the domain level

            // Arrange & Act & Assert
            var action = () => PersonTestDataBuilder.Default()
                .WithFullName(invalidName)
                .Build();

            var ex = Assert.Throws<ArgumentException>(action);
            Assert.Contains("Required input", ex.Message);
            Assert.Contains("was empty", ex.Message);
        }

        [Fact]
        public void WithFullNameTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longFullName = new string('a', 201); // Max length is 200

            // Act & Assert
            var action = () => PersonTestDataBuilder.Default()
                .WithFullName(longFullName)
                .Build();

            var ex = Assert.Throws<ArgumentException>(action);
            Assert.Contains("Input", ex.Message);
            Assert.Contains("too long", ex.Message);
        }
    }
}


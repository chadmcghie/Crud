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
            person.Should().NotBeNull();
            person.Id.Should().NotBeEmpty();
            person.FullName.Should().Be("John Doe");
            person.Phone.Should().Be("555-1234");
            person.Roles.Should().NotBeNull();
            person.Roles.Should().BeEmpty();
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
            person.Should().NotBeNull();
            person.FullName.Should().Be("Jane Doe");
            person.Phone.Should().BeNull();
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var person1 = PersonTestDataBuilder.Default().Build();
            var person2 = PersonTestDataBuilder.Default().Build();

            // Assert
            person1.Id.Should().NotBe(person2.Id);
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
            person.Roles.Should().HaveCount(2);
            person.Roles.Should().Contain(role1);
            person.Roles.Should().Contain(role2);
        }

        [Fact]
        public void WithoutRoles_ShouldHaveEmptyRoleCollection()
        {
            // Arrange & Act
            var person = PersonTestDataBuilder.Default()
                .WithoutRoles()
                .Build();

            // Assert
            person.Roles.Should().NotBeNull();
            person.Roles.Should().BeEmpty();
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

            action.Should().Throw<ArgumentException>()
                .WithMessage("Required input*was empty*");
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

            action.Should().Throw<ArgumentException>()
                .WithMessage("Input*too long*");
        }
    }
}


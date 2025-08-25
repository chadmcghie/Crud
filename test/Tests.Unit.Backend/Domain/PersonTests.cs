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
        public void WithInvalidFullName_ShouldStillCreatePerson_ValidationHandledByDataAnnotations(string invalidName)
        {
            // Note: Entity creation doesn't enforce validation - that's handled by the framework
            // This test documents current behavior
            
            // Arrange & Act
            var person = PersonTestDataBuilder.Default()
                .WithFullName(invalidName)
                .Build();

            // Assert
            person.Should().NotBeNull();
            person.FullName.Should().Be(invalidName);
        }
    }
}

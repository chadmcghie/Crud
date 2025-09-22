using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Api.Dtos;
using FluentAssertions;

namespace Tests.Unit.Backend.Validators;

public class DataAnnotationsPersonValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePersonRequest_Should_Have_Error_When_FullName_Is_Empty(string? fullName)
    {
        // Arrange
        var request = new CreatePersonRequest(fullName!, null, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().ContainSingle(vr =>
            vr.MemberNames.Contains("FullName") &&
            vr.ErrorMessage == "Full name is required");
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_FullName_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('a', 201); // 201 characters
        var request = new CreatePersonRequest(longName, null, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().ContainSingle(vr =>
            vr.MemberNames.Contains("FullName") &&
            vr.ErrorMessage == "Full name cannot exceed 200 characters");
    }

    [Theory]
    [InlineData("123-456-7890")]
    [InlineData("+1 123 456 7890")]
    [InlineData("(123) 456-7890")]
    [InlineData("123.456.7890")]
    public void CreatePersonRequest_Should_Not_Have_Error_When_Phone_Is_Valid(string phone)
    {
        // Arrange
        var request = new CreatePersonRequest("John Doe", phone, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Where(vr => vr.MemberNames.Contains("Phone")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("12345678901234567890")] // Too long
    public void CreatePersonRequest_Should_Have_Error_When_Phone_Is_Invalid(string phone)
    {
        // Arrange
        var request = new CreatePersonRequest("John Doe", phone, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().ContainSingle(vr =>
            vr.MemberNames.Contains("Phone") &&
            vr.ErrorMessage == "Phone number must be a valid format");
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_RoleIds_Contains_Empty_Guid()
    {
        // Arrange
        var roleIds = new[] { Guid.NewGuid(), Guid.Empty };
        var request = new CreatePersonRequest("John Doe", null, roleIds);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().ContainSingle(vr =>
            vr.MemberNames.Contains("RoleIds") &&
            vr.ErrorMessage == "All role IDs must be valid non-empty GUIDs");
    }

    [Fact]
    public void CreatePersonRequest_Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var request = new CreatePersonRequest("John Doe", "123-456-7890", roleIds);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_FullName_Contains_Invalid_Characters()
    {
        // Arrange
        var request = new CreatePersonRequest("John@Doe!", null, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().ContainSingle(vr =>
            vr.MemberNames.Contains("FullName") &&
            vr.ErrorMessage == "Full name contains invalid characters");
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("Mary O'Connor")]
    [InlineData("Jean-Paul Smith")]
    [InlineData("Anna Maria Santos")]
    [InlineData("Dr. Jane Smith")]
    public void CreatePersonRequest_Should_Not_Have_Error_When_FullName_Is_Valid(string fullName)
    {
        // Arrange
        var request = new CreatePersonRequest(fullName, null, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Where(vr => vr.MemberNames.Contains("FullName")).Should().BeEmpty();
    }

    [Fact]
    public void UpdatePersonRequest_Should_Have_Same_Validation_Rules_As_Create()
    {
        // Arrange
        var longName = new string('a', 201);
        var roleIds = new[] { Guid.NewGuid(), Guid.Empty };
        var request = new UpdatePersonRequest("", "invalid-phone", roleIds, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("FullName"));
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("Phone"));
        validationResults.Should().Contain(vr => vr.MemberNames.Contains("RoleIds"));
    }

    private static List<ValidationResult> ValidateObject(object obj)
    {
        var validationContext = new ValidationContext(obj);
        var validationResults = new List<ValidationResult>();

        // For records, we need to validate constructor parameters as well as properties
        Validator.TryValidateObject(obj, validationContext, validationResults, validateAllProperties: true);

        // Check constructor parameters for validation attributes (needed for records)
        var objType = obj.GetType();
        var constructors = objType.GetConstructors();
        var properties = objType.GetProperties();

        // Get the primary constructor (the one with the most parameters)
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (primaryConstructor != null)
        {
            var parameters = primaryConstructor.GetParameters();

            foreach (var parameter in parameters)
            {
                // Find the corresponding property
                var correspondingProperty = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (correspondingProperty != null)
                {
                    var value = correspondingProperty.GetValue(obj);
                    var paramValidationContext = new ValidationContext(obj) { MemberName = correspondingProperty.Name };

                    // Get validation attributes from the constructor parameter
                    var validationAttributes = parameter.GetCustomAttributes<ValidationAttribute>();

                    foreach (var attribute in validationAttributes)
                    {
                        var result = attribute.GetValidationResult(value, paramValidationContext);
                        if (result != null && result != ValidationResult.Success)
                        {
                            validationResults.Add(result);
                        }
                    }
                }
            }
        }

        return validationResults;
    }
}

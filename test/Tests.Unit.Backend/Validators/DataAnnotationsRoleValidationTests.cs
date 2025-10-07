using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Api.Dtos;

namespace Tests.Unit.Backend.Validators;

public class DataAnnotationsRoleValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Is_Empty(string? name)
    {
        // Arrange
        var request = new CreateRoleRequest(name!, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        Assert.Single(validationResults, vr =>
            vr.MemberNames.Contains("Name") &&
            vr.ErrorMessage == "Role name is required");
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('a', 101); // 101 characters
        var request = new CreateRoleRequest(longName, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        Assert.Single(validationResults, vr =>
            vr.MemberNames.Contains("Name") &&
            vr.ErrorMessage == "Role name cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User-Manager")]
    [InlineData("Data_Analyst")]
    [InlineData("Report.Generator")]
    [InlineData("Level 1 Support")]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Name_Is_Valid(string name)
    {
        // Arrange
        var request = new CreateRoleRequest(name, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        Assert.Empty(validationResults.Where(vr => vr.MemberNames.Contains("Name")));
    }

    [Theory]
    [InlineData("Admin@!")]
    [InlineData("User#Manager")]
    [InlineData("Data$Analyst")]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Contains_Invalid_Characters(string name)
    {
        // Arrange
        var request = new CreateRoleRequest(name, null);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        Assert.Single(validationResults, vr =>
            vr.MemberNames.Contains("Name") &&
            vr.ErrorMessage == "Role name can only contain letters, numbers, spaces, hyphens, underscores, and periods");
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var longDescription = new string('a', 501); // 501 characters
        var request = new CreateRoleRequest("Admin", longDescription);

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        Assert.Single(validationResults, vr =>
            vr.MemberNames.Contains("Description") &&
            vr.ErrorMessage == "Description cannot exceed 500 characters");
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Description_Is_Null_Or_Empty()
    {
        // Arrange
        var request1 = new CreateRoleRequest("Admin", null);
        var request2 = new CreateRoleRequest("Admin", "");
        var request3 = new CreateRoleRequest("Admin", "   ");

        // Act & Assert
        Assert.Empty(ValidateObject(request1).Where(vr => vr.MemberNames.Contains("Description")));
        Assert.Empty(ValidateObject(request2).Where(vr => vr.MemberNames.Contains("Description")));
        Assert.Empty(ValidateObject(request3).Where(vr => vr.MemberNames.Contains("Description")));
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var request = new CreateRoleRequest("Administrator", "Manages system settings and user accounts");

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        Assert.Empty(validationResults);
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

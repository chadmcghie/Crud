using System.ComponentModel.DataAnnotations;
using Api.Dtos;
using FluentAssertions;

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
        validationResults.Should().ContainSingle(vr =>
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
        validationResults.Should().ContainSingle(vr =>
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
        validationResults.Where(vr => vr.MemberNames.Contains("Name")).Should().BeEmpty();
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
        validationResults.Should().ContainSingle(vr =>
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
        validationResults.Should().ContainSingle(vr =>
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
        ValidateObject(request1).Where(vr => vr.MemberNames.Contains("Description")).Should().BeEmpty();
        ValidateObject(request2).Where(vr => vr.MemberNames.Contains("Description")).Should().BeEmpty();
        ValidateObject(request3).Where(vr => vr.MemberNames.Contains("Description")).Should().BeEmpty();
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var request = new CreateRoleRequest("Administrator", "Manages system settings and user accounts");

        // Act
        var validationResults = ValidateObject(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateObject(object obj)
    {
        var validationContext = new ValidationContext(obj);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(obj, validationContext, validationResults, true);
        return validationResults;
    }
}

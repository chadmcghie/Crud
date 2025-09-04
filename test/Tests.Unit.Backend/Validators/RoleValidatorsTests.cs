using Api.Dtos;
using Api.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests.Unit.Backend.Validators;

public class RoleValidatorsTests
{
    private readonly CreateRoleRequestValidator _createValidator = new();
    private readonly UpdateRoleRequestValidator _updateValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Is_Empty(string? name)
    {
        // Arrange
        var request = new CreateRoleRequest(name!, null);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Role name is required");
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('a', 101); // 101 characters
        var request = new CreateRoleRequest(longName, null);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Role name cannot exceed 100 characters");
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

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("Admin@!")]
    [InlineData("User#Manager")]
    [InlineData("Data$Analyst")]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Contains_Invalid_Characters(string name)
    {
        // Arrange
        var request = new CreateRoleRequest(name, null);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Role name can only contain letters, numbers, spaces, hyphens, underscores, and periods");
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var longDescription = new string('a', 501); // 501 characters
        var request = new CreateRoleRequest("Admin", longDescription);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 500 characters");
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Description_Is_Null_Or_Empty()
    {
        // Arrange
        var request1 = new CreateRoleRequest("Admin", null);
        var request2 = new CreateRoleRequest("Admin", "");
        var request3 = new CreateRoleRequest("Admin", "   ");

        // Act & Assert
        _createValidator.TestValidate(request1).ShouldNotHaveValidationErrorFor(x => x.Description);
        _createValidator.TestValidate(request2).ShouldNotHaveValidationErrorFor(x => x.Description);
        _createValidator.TestValidate(request3).ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var request = new CreateRoleRequest("Administrator", "Manages system settings and user accounts");

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateRoleRequest_Should_Have_Same_Validation_Rules_As_Create()
    {
        // Arrange
        var longName = new string('a', 101);
        var longDescription = new string('b', 501);
        var request = new UpdateRoleRequest("", longDescription);

        // Act & Assert
        var result = _updateValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

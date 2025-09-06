using Api.Dtos;
using Api.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests.Unit.Backend.Validators;

public class PersonValidatorsTests
{
    private readonly CreatePersonRequestValidator _createValidator = new();
    private readonly UpdatePersonRequestValidator _updateValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePersonRequest_Should_Have_Error_When_FullName_Is_Empty(string? fullName)
    {
        // Arrange
        var request = new CreatePersonRequest(fullName!, null, null);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName)
              .WithErrorMessage("Full name is required");
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_FullName_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('a', 201); // 201 characters
        var request = new CreatePersonRequest(longName, null, null);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName)
              .WithErrorMessage("Full name cannot exceed 200 characters");
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

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("12345678901234567890")] // Too long
    public void CreatePersonRequest_Should_Have_Error_When_Phone_Is_Invalid(string phone)
    {
        // Arrange
        var request = new CreatePersonRequest("John Doe", phone, null);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number must be a valid format");
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_RoleIds_Contains_Empty_Guid()
    {
        // Arrange
        var roleIds = new[] { Guid.NewGuid(), Guid.Empty };
        var request = new CreatePersonRequest("John Doe", null, roleIds);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
              .WithErrorMessage("All role IDs must be valid non-empty GUIDs");
    }

    [Fact]
    public void CreatePersonRequest_Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var request = new CreatePersonRequest("John Doe", "123-456-7890", roleIds);

        // Act & Assert
        var result = _createValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // Similar tests for UpdatePersonRequestValidator
    [Fact]
    public void UpdatePersonRequest_Should_Have_Same_Validation_Rules_As_Create()
    {
        // Arrange
        var request = new UpdatePersonRequest("", "invalid-phone", new[] { Guid.Empty });

        // Act & Assert
        var result = _updateValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }
}

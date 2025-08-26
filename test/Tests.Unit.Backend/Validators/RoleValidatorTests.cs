using Api.Dtos;
using Api.Validators;
using Xunit;

namespace Tests.Unit.Backend.Validators;

public class RoleValidatorTests
{
    private readonly CreateRoleRequestValidator _createValidator;
    private readonly UpdateRoleRequestValidator _updateValidator;

    public RoleValidatorTests()
    {
        _createValidator = new CreateRoleRequestValidator();
        _updateValidator = new UpdateRoleRequestValidator();
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var model = new CreateRoleRequest("", "Test description");

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Name));
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var model = new CreateRoleRequest(new string('A', 101), "Test description");

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Name));
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Name_Is_Valid()
    {
        // Arrange
        var model = new CreateRoleRequest("Administrator", "Test description");

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.Name));
    }

    [Fact]
    public void CreateRoleRequest_Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var model = new CreateRoleRequest("Admin", new string('A', 501));

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Description));
    }

    [Fact]
    public void CreateRoleRequest_Should_Not_Have_Error_When_Description_Is_Null()
    {
        // Arrange
        var model = new CreateRoleRequest("Admin", null);

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.Description));
    }

    [Fact]
    public void UpdateRoleRequest_Should_Have_Same_Validation_As_Create()
    {
        // Arrange
        var validModel = new UpdateRoleRequest("Manager", "Manages team operations");
        var invalidModel = new UpdateRoleRequest("", new string('A', 501));

        // Act
        var validResult = _updateValidator.Validate(validModel);
        var invalidResult = _updateValidator.Validate(invalidModel);

        // Assert
        Assert.DoesNotContain(validResult.Errors, e => e.PropertyName == nameof(validModel.Name));
        Assert.DoesNotContain(validResult.Errors, e => e.PropertyName == nameof(validModel.Description));

        Assert.False(invalidResult.IsValid);
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == nameof(invalidModel.Name));
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == nameof(invalidModel.Description));
    }
}
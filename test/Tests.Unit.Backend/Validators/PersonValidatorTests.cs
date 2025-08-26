using Api.Dtos;
using Api.Validators;
using Xunit;

namespace Tests.Unit.Backend.Validators;

public class PersonValidatorTests
{
    private readonly CreatePersonRequestValidator _createValidator;
    private readonly UpdatePersonRequestValidator _updateValidator;

    public PersonValidatorTests()
    {
        _createValidator = new CreatePersonRequestValidator();
        _updateValidator = new UpdatePersonRequestValidator();
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_FullName_Is_Empty()
    {
        // Arrange
        var model = new CreatePersonRequest("", null, new List<Guid>());

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.FullName));
    }

    [Fact]
    public void CreatePersonRequest_Should_Not_Have_Error_When_FullName_Is_Valid()
    {
        // Arrange
        var model = new CreatePersonRequest("John Doe", null, new List<Guid>());

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.FullName));
    }

    [Fact]
    public void CreatePersonRequest_Should_Have_Error_When_Phone_Is_Invalid()
    {
        // Arrange
        var model = new CreatePersonRequest("John Doe", "invalid-phone", new List<Guid>());

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Phone));
    }

    [Fact]
    public void CreatePersonRequest_Should_Not_Have_Error_When_Phone_Is_Valid()
    {
        // Arrange
        var model = new CreatePersonRequest("John Doe", "1234567890", new List<Guid>());

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.Phone));
    }

    [Fact]
    public void CreatePersonRequest_Should_Not_Have_Error_When_Phone_Is_Null()
    {
        // Arrange
        var model = new CreatePersonRequest("John Doe", null, new List<Guid>());

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.Phone));
    }

    [Fact]
    public void UpdatePersonRequest_Should_Have_Error_When_FullName_Is_Empty()
    {
        // Arrange
        var model = new UpdatePersonRequest("", null, new List<Guid>());

        // Act
        var result = _updateValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.FullName));
    }

    [Fact]
    public void UpdatePersonRequest_Should_Not_Have_Error_When_Valid()
    {
        // Arrange
        var roleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var model = new UpdatePersonRequest("Jane Doe", "5551234567", roleIds);

        // Act
        var result = _updateValidator.Validate(model);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.FullName));
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.Phone));
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.RoleIds));
    }
}
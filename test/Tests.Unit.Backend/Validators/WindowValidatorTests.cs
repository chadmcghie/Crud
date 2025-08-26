using Api.Dtos;
using Api.Validators;
using Xunit;

namespace Tests.Unit.Backend.Validators;

public class WindowValidatorTests
{
    private readonly CreateWindowRequestValidator _createValidator;
    private readonly UpdateWindowRequestValidator _updateValidator;

    public WindowValidatorTests()
    {
        _createValidator = new CreateWindowRequestValidator();
        _updateValidator = new UpdateWindowRequestValidator();
    }

    [Fact]
    public void CreateWindowRequest_Should_Have_Error_When_Required_Fields_Are_Empty()
    {
        // Arrange
        var model = new CreateWindowRequest(
            "", // empty name
            null,
            1.0, 1.0, 1.0,
            "", // empty frame type
            null,
            "", // empty glazing type
            null,
            null, null, null, null, null, null, null, null, null, null, null, null
        );

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.FrameType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.GlazingType));
    }

    [Fact]
    public void CreateWindowRequest_Should_Have_Error_When_Dimensions_Are_Out_Of_Range()
    {
        // Arrange
        var model = new CreateWindowRequest(
            "Valid Name",
            null,
            0.05, // width too small
            150,  // height too large
            2000, // area too large
            "Valid Frame",
            null,
            "Valid Glazing",
            null,
            null, null, null, null, null, null, null, null, null, null, null, null
        );

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Width));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Height));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Area));
    }

    [Fact]
    public void CreateWindowRequest_Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var model = new CreateWindowRequest(
            "Standard Double-Hung Window",
            "Energy efficient window for residential use",
            3.5, 4.0, 14.0,
            "Vinyl",
            "White finish with screen",
            "Double",
            "Low-E coating with argon fill",
            0.28, 0.25, 0.65, 0.2, "Yes", "NFRC-123", "South", "Living Room", "Replacement", "Double-hung", true, false
        );

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void CreateWindowRequest_Should_Have_Error_When_Optional_Numeric_Values_Are_Out_Of_Range()
    {
        // Arrange
        var model = new CreateWindowRequest(
            "Valid Name",
            null,
            2.0, 3.0, 6.0,
            "Valid Frame",
            null,
            "Valid Glazing",
            null,
            15.0, // UValue too high
            2.0,  // SHGC too high
            -0.1, // VT too low
            150,  // Air leakage too high
            null, null, null, null, null, null, null, null
        );

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.UValue));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.SolarHeatGainCoefficient));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.VisibleTransmittance));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.AirLeakage));
    }

    [Fact]
    public void CreateWindowRequest_Should_Have_Error_When_Strings_Exceed_MaxLength()
    {
        // Arrange
        var model = new CreateWindowRequest(
            new string('A', 201), // name too long
            new string('B', 1001), // description too long
            2.0, 3.0, 6.0,
            new string('C', 101), // frame type too long
            new string('D', 501), // frame details too long
            new string('E', 101), // glazing type too long
            new string('F', 501), // glazing details too long
            null, null, null, null,
            new string('G', 51), // energy star rating too long
            new string('H', 51), // NFRC rating too long
            new string('I', 51), // orientation too long
            new string('J', 101), // location too long
            new string('K', 51), // installation type too long
            new string('L', 101), // operation type too long
            null, null
        );

        // Act
        var result = _createValidator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Description));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.FrameType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.FrameDetails));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.GlazingType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.GlazingDetails));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.EnergyStarRating));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.NFRCRating));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Orientation));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.Location));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.InstallationType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.OperationType));
    }
}
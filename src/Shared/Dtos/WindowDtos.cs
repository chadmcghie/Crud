using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;

public record CreateWindowRequest(
    [param: Required] string Name,
    [param: MaxLength(1000)] string? Description,
    [param: Range(0.1, 100)] double Width,
    [param: Range(0.1, 100)] double Height,
    [param: Range(0.01, 1000)] double Area,
    [param: Required] string FrameType,
    [param: MaxLength(500)] string? FrameDetails,
    [param: Required] string GlazingType,
    [param: MaxLength(500)] string? GlazingDetails,
    [param: Range(0, 10)] double? UValue,
    [param: Range(0, 1)] double? SolarHeatGainCoefficient,
    [param: Range(0, 1)] double? VisibleTransmittance,
    [param: Range(0, 100)] double? AirLeakage,
    [param: MaxLength(50)] string? EnergyStarRating,
    [param: MaxLength(50)] string? NFRCRating,
    [param: MaxLength(50)] string? Orientation,
    [param: MaxLength(100)] string? Location,
    [param: MaxLength(50)] string? InstallationType,
    [param: MaxLength(100)] string? OperationType,
    bool? HasScreens,
    bool? HasStormWindows
);

public record UpdateWindowRequest(
    [param: Required] string Name,
    [param: MaxLength(1000)] string? Description,
    [param: Range(0.1, 100)] double Width,
    [param: Range(0.1, 100)] double Height,
    [param: Range(0.01, 1000)] double Area,
    [param: Required] string FrameType,
    [param: MaxLength(500)] string? FrameDetails,
    [param: Required] string GlazingType,
    [param: MaxLength(500)] string? GlazingDetails,
    [param: Range(0, 10)] double? UValue,
    [param: Range(0, 1)] double? SolarHeatGainCoefficient,
    [param: Range(0, 1)] double? VisibleTransmittance,
    [param: Range(0, 100)] double? AirLeakage,
    [param: MaxLength(50)] string? EnergyStarRating,
    [param: MaxLength(50)] string? NFRCRating,
    [param: MaxLength(50)] string? Orientation,
    [param: MaxLength(100)] string? Location,
    [param: MaxLength(50)] string? InstallationType,
    [param: MaxLength(100)] string? OperationType,
    bool? HasScreens,
    bool? HasStormWindows
);

public record WindowResponse(
    Guid Id,
    string Name,
    string? Description,
    double Width,
    double Height,
    double Area,
    string FrameType,
    string? FrameDetails,
    string GlazingType,
    string? GlazingDetails,
    double? UValue,
    double? SolarHeatGainCoefficient,
    double? VisibleTransmittance,
    double? AirLeakage,
    string? EnergyStarRating,
    string? NFRCRating,
    string? Orientation,
    string? Location,
    string? InstallationType,
    string? OperationType,
    bool? HasScreens,
    bool? HasStormWindows,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
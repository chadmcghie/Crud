using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public record CreateWallRequest(
    [param: Required] string Name,
    [param: MaxLength(1000)] string? Description,
    [param: Range(0.1, 1000)] double Length,
    [param: Range(0.1, 100)] double Height,
    [param: Range(0.1, 100)] double Thickness,
    [param: Required] string AssemblyType,
    [param: MaxLength(1000)] string? AssemblyDetails,
    [param: Range(0, 100)] double? RValue,
    [param: Range(0, 10)] double? UValue,
    [param: MaxLength(2000)] string? MaterialLayers,
    [param: MaxLength(50)] string? Orientation,
    [param: MaxLength(50)] string? Location
);

public record UpdateWallRequest(
    [param: Required] string Name,
    [param: MaxLength(1000)] string? Description,
    [param: Range(0.1, 1000)] double Length,
    [param: Range(0.1, 100)] double Height,
    [param: Range(0.1, 100)] double Thickness,
    [param: Required] string AssemblyType,
    [param: MaxLength(1000)] string? AssemblyDetails,
    [param: Range(0, 100)] double? RValue,
    [param: Range(0, 10)] double? UValue,
    [param: MaxLength(2000)] string? MaterialLayers,
    [param: MaxLength(50)] string? Orientation,
    [param: MaxLength(50)] string? Location
);

public record WallResponse(
    Guid Id,
    string Name,
    string? Description,
    double Length,
    double Height,
    double Thickness,
    string AssemblyType,
    string? AssemblyDetails,
    double? RValue,
    double? UValue,
    string? MaterialLayers,
    string? Orientation,
    string? Location,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

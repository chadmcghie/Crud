using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public record CreateWallCommand(
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
    string? Location
) : IRequest<Wall>;

public record UpdateWallCommand(
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
    string? Location
) : IRequest;

public record DeleteWallCommand(Guid Id) : IRequest;

using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public record CreateWindowCommand(
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
    bool? HasStormWindows
) : IRequest<Window>;

public record UpdateWindowCommand(
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
    bool? HasStormWindows
) : IRequest;

public record DeleteWindowCommand(Guid Id) : IRequest;

using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public class CreateWindowCommandHandler(IWindowRepository windowRepository) : IRequestHandler<CreateWindowCommand, Window>
{
    public async Task<Window> Handle(CreateWindowCommand request, CancellationToken cancellationToken)
    {
        // Note: Area is now calculated from Width * Height, so we don't pass the Area parameter
        var window = Window.Create(
            request.Name,
            request.Width,
            request.Height,
            request.FrameType,
            request.GlazingType,
            request.Description);

        // Update additional properties using domain methods
        if (request.FrameDetails != null)
        {
            window.UpdateFrameProperties(request.FrameType, request.FrameDetails);
        }

        if (request.GlazingDetails != null)
        {
            window.UpdateGlazingProperties(request.GlazingType, request.GlazingDetails);
        }

        if (request.UValue.HasValue || request.SolarHeatGainCoefficient.HasValue ||
            request.VisibleTransmittance.HasValue || request.AirLeakage.HasValue)
        {
            window.UpdateEnergyProperties(
                request.UValue,
                request.SolarHeatGainCoefficient,
                request.VisibleTransmittance,
                request.AirLeakage);
        }

        if (request.EnergyStarRating != null || request.NFRCRating != null)
        {
            window.UpdatePerformanceRatings(request.EnergyStarRating, request.NFRCRating);
        }

        if (request.Location != null || request.Orientation != null || request.InstallationType != null)
        {
            window.UpdateLocationAndOrientation(
                request.Location,
                request.Orientation,
                request.InstallationType);
        }

        if (request.OperationType != null || request.HasScreens.HasValue || request.HasStormWindows.HasValue)
        {
            window.UpdateOperationalProperties(
                request.OperationType,
                request.HasScreens,
                request.HasStormWindows);
        }

        return await windowRepository.AddAsync(window, cancellationToken);
    }
}

public class UpdateWindowCommandHandler(IWindowRepository windowRepository) : IRequestHandler<UpdateWindowCommand>
{
    public async Task Handle(UpdateWindowCommand request, CancellationToken cancellationToken)
    {
        var window = await windowRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Window {request.Id} not found");

        // Update properties using domain methods
        window.UpdateName(request.Name);
        window.UpdateDescription(request.Description);
        window.UpdateDimensions(request.Width, request.Height);
        window.UpdateFrameProperties(request.FrameType, request.FrameDetails);
        window.UpdateGlazingProperties(request.GlazingType, request.GlazingDetails);
        window.UpdateEnergyProperties(
            request.UValue,
            request.SolarHeatGainCoefficient,
            request.VisibleTransmittance,
            request.AirLeakage);
        window.UpdatePerformanceRatings(request.EnergyStarRating, request.NFRCRating);
        window.UpdateLocationAndOrientation(
            request.Location,
            request.Orientation,
            request.InstallationType);
        window.UpdateOperationalProperties(
            request.OperationType,
            request.HasScreens,
            request.HasStormWindows);

        await windowRepository.UpdateAsync(window, cancellationToken);
    }
}

public class DeleteWindowCommandHandler(IWindowRepository windowRepository) : IRequestHandler<DeleteWindowCommand>
{
    public async Task Handle(DeleteWindowCommand request, CancellationToken cancellationToken)
    {
        await windowRepository.DeleteAsync(request.Id, cancellationToken);
    }
}

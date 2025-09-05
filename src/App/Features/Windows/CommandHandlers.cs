using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public class CreateWindowCommandHandler(IWindowRepository windowRepository) : IRequestHandler<CreateWindowCommand, Window>
{
    public async Task<Window> Handle(CreateWindowCommand request, CancellationToken cancellationToken)
    {
        var window = new Window
        {
            Name = request.Name,
            Description = request.Description,
            Width = request.Width,
            Height = request.Height,
            Area = request.Area,
            FrameType = request.FrameType,
            FrameDetails = request.FrameDetails,
            GlazingType = request.GlazingType,
            GlazingDetails = request.GlazingDetails,
            UValue = request.UValue,
            SolarHeatGainCoefficient = request.SolarHeatGainCoefficient,
            VisibleTransmittance = request.VisibleTransmittance,
            AirLeakage = request.AirLeakage,
            EnergyStarRating = request.EnergyStarRating,
            NFRCRating = request.NFRCRating,
            Orientation = request.Orientation,
            Location = request.Location,
            InstallationType = request.InstallationType,
            OperationType = request.OperationType,
            HasScreens = request.HasScreens,
            HasStormWindows = request.HasStormWindows
        };
        return await windowRepository.AddAsync(window, cancellationToken);
    }
}

public class UpdateWindowCommandHandler(IWindowRepository windowRepository) : IRequestHandler<UpdateWindowCommand>
{
    public async Task Handle(UpdateWindowCommand request, CancellationToken cancellationToken)
    {
        var window = await windowRepository.GetAsync(request.Id, cancellationToken) 
            ?? throw new KeyNotFoundException($"Window {request.Id} not found");

        window.Name = request.Name;
        window.Description = request.Description;
        window.Width = request.Width;
        window.Height = request.Height;
        window.Area = request.Area;
        window.FrameType = request.FrameType;
        window.FrameDetails = request.FrameDetails;
        window.GlazingType = request.GlazingType;
        window.GlazingDetails = request.GlazingDetails;
        window.UValue = request.UValue;
        window.SolarHeatGainCoefficient = request.SolarHeatGainCoefficient;
        window.VisibleTransmittance = request.VisibleTransmittance;
        window.AirLeakage = request.AirLeakage;
        window.EnergyStarRating = request.EnergyStarRating;
        window.NFRCRating = request.NFRCRating;
        window.Orientation = request.Orientation;
        window.Location = request.Location;
        window.InstallationType = request.InstallationType;
        window.OperationType = request.OperationType;
        window.HasScreens = request.HasScreens;
        window.HasStormWindows = request.HasStormWindows;
        window.UpdatedAt = DateTime.UtcNow;

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

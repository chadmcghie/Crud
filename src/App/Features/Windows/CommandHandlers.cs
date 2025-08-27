using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public class CreateWindowCommandHandler(IWindowService windowService) : IRequestHandler<CreateWindowCommand, Window>
{
    public async Task<Window> Handle(CreateWindowCommand request, CancellationToken cancellationToken)
    {
        return await windowService.CreateAsync(
            request.Name,
            request.Description,
            request.Width,
            request.Height,
            request.Area,
            request.FrameType,
            request.FrameDetails,
            request.GlazingType,
            request.GlazingDetails,
            request.UValue,
            request.SolarHeatGainCoefficient,
            request.VisibleTransmittance,
            request.AirLeakage,
            request.EnergyStarRating,
            request.NFRCRating,
            request.Orientation,
            request.Location,
            request.InstallationType,
            request.OperationType,
            request.HasScreens,
            request.HasStormWindows,
            cancellationToken
        );
    }
}

public class UpdateWindowCommandHandler(IWindowService windowService) : IRequestHandler<UpdateWindowCommand>
{
    public async Task Handle(UpdateWindowCommand request, CancellationToken cancellationToken)
    {
        await windowService.UpdateAsync(
            request.Id,
            request.Name,
            request.Description,
            request.Width,
            request.Height,
            request.Area,
            request.FrameType,
            request.FrameDetails,
            request.GlazingType,
            request.GlazingDetails,
            request.UValue,
            request.SolarHeatGainCoefficient,
            request.VisibleTransmittance,
            request.AirLeakage,
            request.EnergyStarRating,
            request.NFRCRating,
            request.Orientation,
            request.Location,
            request.InstallationType,
            request.OperationType,
            request.HasScreens,
            request.HasStormWindows,
            cancellationToken
        );
    }
}

public class DeleteWindowCommandHandler(IWindowService windowService) : IRequestHandler<DeleteWindowCommand>
{
    public async Task Handle(DeleteWindowCommand request, CancellationToken cancellationToken)
    {
        await windowService.DeleteAsync(request.Id, cancellationToken);
    }
}
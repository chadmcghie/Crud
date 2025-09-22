using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public class CreateWallCommandHandler(IWallRepository wallRepository) : IRequestHandler<CreateWallCommand, Wall>
{
    public async Task<Wall> Handle(CreateWallCommand request, CancellationToken cancellationToken)
    {
        var wall = Wall.Create(
            request.Name,
            request.Length,
            request.Height,
            request.Thickness,
            request.AssemblyType,
            request.Description);

        // Update additional properties using domain methods
        if (request.AssemblyDetails != null)
        {
            wall.UpdateAssemblyDetails(request.AssemblyDetails);
        }

        if (request.RValue.HasValue || request.UValue.HasValue)
        {
            wall.UpdateEnergyProperties(request.RValue, request.UValue);
        }

        if (request.MaterialLayers != null)
        {
            wall.UpdateMaterialLayers(request.MaterialLayers);
        }

        if (request.Location != null || request.Orientation != null)
        {
            wall.UpdateLocationAndOrientation(request.Location, request.Orientation);
        }

        return await wallRepository.AddAsync(wall, cancellationToken);
    }
}

public class UpdateWallCommandHandler(IWallRepository wallRepository) : IRequestHandler<UpdateWallCommand>
{
    public async Task Handle(UpdateWallCommand request, CancellationToken cancellationToken)
    {
        var wall = await wallRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Wall {request.Id} not found");

        // Update properties using domain methods
        wall.UpdateName(request.Name);
        wall.UpdateDescription(request.Description);
        wall.UpdateDimensions(request.Length, request.Height, request.Thickness);
        wall.UpdateAssemblyType(request.AssemblyType);
        wall.UpdateAssemblyDetails(request.AssemblyDetails);
        wall.UpdateEnergyProperties(request.RValue, request.UValue);
        wall.UpdateMaterialLayers(request.MaterialLayers);
        wall.UpdateLocationAndOrientation(request.Location, request.Orientation);

        await wallRepository.UpdateAsync(wall, cancellationToken);
    }
}

public class DeleteWallCommandHandler(IWallRepository wallRepository) : IRequestHandler<DeleteWallCommand>
{
    public async Task Handle(DeleteWallCommand request, CancellationToken cancellationToken)
    {
        await wallRepository.DeleteAsync(request.Id, cancellationToken);
    }
}

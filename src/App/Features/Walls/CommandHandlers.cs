using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public class CreateWallCommandHandler(IWallRepository wallRepository) : IRequestHandler<CreateWallCommand, Wall>
{
    public async Task<Wall> Handle(CreateWallCommand request, CancellationToken cancellationToken)
    {
        var wall = new Wall
        {
            Name = request.Name,
            Description = request.Description,
            Length = request.Length,
            Height = request.Height,
            Thickness = request.Thickness,
            AssemblyType = request.AssemblyType,
            AssemblyDetails = request.AssemblyDetails,
            RValue = request.RValue,
            UValue = request.UValue,
            MaterialLayers = request.MaterialLayers,
            Orientation = request.Orientation,
            Location = request.Location
        };
        return await wallRepository.AddAsync(wall, cancellationToken);
    }
}

public class UpdateWallCommandHandler(IWallRepository wallRepository) : IRequestHandler<UpdateWallCommand>
{
    public async Task Handle(UpdateWallCommand request, CancellationToken cancellationToken)
    {
        var wall = await wallRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Wall {request.Id} not found");

        wall.Name = request.Name;
        wall.Description = request.Description;
        wall.Length = request.Length;
        wall.Height = request.Height;
        wall.Thickness = request.Thickness;
        wall.AssemblyType = request.AssemblyType;
        wall.AssemblyDetails = request.AssemblyDetails;
        wall.RValue = request.RValue;
        wall.UValue = request.UValue;
        wall.MaterialLayers = request.MaterialLayers;
        wall.Orientation = request.Orientation;
        wall.Location = request.Location;
        wall.UpdatedAt = DateTime.UtcNow;

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

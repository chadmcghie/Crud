using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public class CreateWallCommandHandler(IWallService wallService) : IRequestHandler<CreateWallCommand, Wall>
{
    public async Task<Wall> Handle(CreateWallCommand request, CancellationToken cancellationToken)
    {
        return await wallService.CreateAsync(
            request.Name,
            request.Description,
            request.Length,
            request.Height,
            request.Thickness,
            request.AssemblyType,
            request.AssemblyDetails,
            request.RValue,
            request.UValue,
            request.MaterialLayers,
            request.Orientation,
            request.Location,
            cancellationToken
        );
    }
}

public class UpdateWallCommandHandler(IWallService wallService) : IRequestHandler<UpdateWallCommand>
{
    public async Task Handle(UpdateWallCommand request, CancellationToken cancellationToken)
    {
        await wallService.UpdateAsync(
            request.Id,
            request.Name,
            request.Description,
            request.Length,
            request.Height,
            request.Thickness,
            request.AssemblyType,
            request.AssemblyDetails,
            request.RValue,
            request.UValue,
            request.MaterialLayers,
            request.Orientation,
            request.Location,
            cancellationToken
        );
    }
}

public class DeleteWallCommandHandler(IWallService wallService) : IRequestHandler<DeleteWallCommand>
{
    public async Task Handle(DeleteWallCommand request, CancellationToken cancellationToken)
    {
        await wallService.DeleteAsync(request.Id, cancellationToken);
    }
}

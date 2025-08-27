using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Roles;

public class CreateRoleCommandHandler(IRoleService roleService) : IRequestHandler<CreateRoleCommand, Role>
{
    public async Task<Role> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        return await roleService.CreateAsync(request.Name, request.Description, cancellationToken);
    }
}

public class UpdateRoleCommandHandler(IRoleService roleService) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        await roleService.UpdateAsync(request.Id, request.Name, request.Description, cancellationToken);
    }
}

public class DeleteRoleCommandHandler(IRoleService roleService) : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(request.Id, cancellationToken);
    }
}
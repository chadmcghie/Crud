using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Commands.Roles;

public class CreateRoleCommandHandler(IRoleService roleService) : IRequestHandler<CreateRoleCommand, Role>
{
    public async Task<Role> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        return await roleService.CreateAsync(request.Name, request.Description, cancellationToken);
    }
}
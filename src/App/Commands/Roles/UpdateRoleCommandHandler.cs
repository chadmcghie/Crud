using App.Abstractions;
using MediatR;

namespace App.Commands.Roles;

public class UpdateRoleCommandHandler(IRoleService roleService) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        await roleService.UpdateAsync(request.Id, request.Name, request.Description, cancellationToken);
    }
}
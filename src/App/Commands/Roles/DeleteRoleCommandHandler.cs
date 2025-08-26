using App.Abstractions;
using MediatR;

namespace App.Commands.Roles;

public class DeleteRoleCommandHandler(IRoleService roleService) : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(request.Id, cancellationToken);
    }
}
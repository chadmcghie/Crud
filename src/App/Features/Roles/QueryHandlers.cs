using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Roles;

public class GetRoleQueryHandler(IRoleService roleService) : IRequestHandler<GetRoleQuery, Role?>
{
    public async Task<Role?> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        return await roleService.GetAsync(request.Id, cancellationToken);
    }
}

public class ListRolesQueryHandler(IRoleService roleService) : IRequestHandler<ListRolesQuery, IReadOnlyList<Role>>
{
    public async Task<IReadOnlyList<Role>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        return await roleService.ListAsync(cancellationToken);
    }
}

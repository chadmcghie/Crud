using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Queries.Roles;

public class ListRolesQueryHandler(IRoleService roleService) : IRequestHandler<ListRolesQuery, IReadOnlyList<Role>>
{
    public async Task<IReadOnlyList<Role>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        return await roleService.ListAsync(cancellationToken);
    }
}
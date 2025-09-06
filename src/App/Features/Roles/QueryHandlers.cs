using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Roles;

public class GetRoleQueryHandler(IRoleRepository roleRepository) : IRequestHandler<GetRoleQuery, Role?>
{
    public async Task<Role?> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        return await roleRepository.GetAsync(request.Id, cancellationToken);
    }
}

public class ListRolesQueryHandler(IRoleRepository roleRepository) : IRequestHandler<ListRolesQuery, IReadOnlyList<Role>>
{
    public async Task<IReadOnlyList<Role>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        return await roleRepository.ListAsync(cancellationToken);
    }
}

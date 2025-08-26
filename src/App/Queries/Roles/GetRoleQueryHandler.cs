using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Queries.Roles;

public class GetRoleQueryHandler(IRoleService roleService) : IRequestHandler<GetRoleQuery, Role?>
{
    public async Task<Role?> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        return await roleService.GetAsync(request.Id, cancellationToken);
    }
}
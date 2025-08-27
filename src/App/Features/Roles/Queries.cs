using Domain.Entities;
using MediatR;

namespace App.Features.Roles;

public record GetRoleQuery(Guid Id) : IRequest<Role?>;

public record ListRolesQuery : IRequest<IReadOnlyList<Role>>;
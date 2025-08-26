using Domain.Entities;
using MediatR;

namespace App.Queries.Roles;

public record ListRolesQuery : IRequest<IReadOnlyList<Role>>;
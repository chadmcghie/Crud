using Domain.Entities;
using MediatR;

namespace App.Queries.Roles;

public record GetRoleQuery(Guid Id) : IRequest<Role?>;
using Domain.Entities;
using MediatR;

namespace App.Features.Roles;

public record CreateRoleCommand(string Name, string? Description) : IRequest<Role>;

public record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest;

public record DeleteRoleCommand(Guid Id) : IRequest;
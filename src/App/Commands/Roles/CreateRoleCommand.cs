using Domain.Entities;
using MediatR;

namespace App.Commands.Roles;

public record CreateRoleCommand(string Name, string? Description) : IRequest<Role>;
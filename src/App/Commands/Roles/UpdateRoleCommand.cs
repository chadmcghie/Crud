using MediatR;

namespace App.Commands.Roles;

public record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest;
using MediatR;

namespace App.Commands.Roles;

public record DeleteRoleCommand(Guid Id) : IRequest;
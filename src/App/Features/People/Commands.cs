using Domain.Entities;
using MediatR;

namespace App.Features.People;

public record CreatePersonCommand(
    string FullName,
    string? Phone,
    IEnumerable<Guid>? RoleIds
) : IRequest<Person>;

public record UpdatePersonCommand(
    Guid Id,
    string FullName,
    string? Phone,
    IEnumerable<Guid>? RoleIds,
    byte[]? RowVersion
) : IRequest;

public record DeletePersonCommand(Guid Id) : IRequest;

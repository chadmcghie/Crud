using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public record CreatePersonRequest(
    [param: Required] string FullName,
    [param: Phone] string? Phone,
    [param: MinLength(0)] IEnumerable<Guid>? RoleIds
);

public record UpdatePersonRequest(
    [param: Required] string FullName,
    [param: Phone] string? Phone,
    [param: MinLength(0)] IEnumerable<Guid>? RoleIds
);

public record PersonResponse(Guid Id, string FullName, string? Phone, IEnumerable<RoleResponse> Roles);

public record RoleResponse(Guid Id, string Name, string? Description);

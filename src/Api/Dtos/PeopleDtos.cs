using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public record CreatePersonRequest(
    [property: Required] string FullName,
    [property: Phone] string? Phone,
    [property: MinLength(0)] IEnumerable<Guid>? RoleIds
);

public record UpdatePersonRequest(
    [property: Required] string FullName,
    [property: Phone] string? Phone,
    [property: MinLength(0)] IEnumerable<Guid>? RoleIds
);

public record PersonResponse(Guid Id, string FullName, string? Phone, IEnumerable<RoleResponse> Roles);

public record RoleResponse(Guid Id, string Name, string? Description);

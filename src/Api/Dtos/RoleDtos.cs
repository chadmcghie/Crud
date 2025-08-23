using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public record CreateRoleRequest(
    [property: Required] string Name,
    string? Description
);

public record UpdateRoleRequest(
    [property: Required] string Name,
    string? Description
);

public record RoleDto(Guid Id, string Name, string? Description);

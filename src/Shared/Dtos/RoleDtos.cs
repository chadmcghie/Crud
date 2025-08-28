using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;

public record CreateRoleRequest(
    [param: Required] string Name,
    string? Description
);

public record UpdateRoleRequest(
    [param: Required] string Name,
    string? Description
);

public record RoleDto(Guid Id, string Name, string? Description);

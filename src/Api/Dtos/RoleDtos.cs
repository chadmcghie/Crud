using System.ComponentModel.DataAnnotations;
using App.Validation;

namespace Api.Dtos;

public record CreateRoleRequest(
    [property: Required(ErrorMessage = "Role name is required")]
    [property: StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters")]
    [property: AllowedCharacters(@"^[a-zA-Z0-9\s\-_\.]+$", ErrorMessage = "Role name can only contain letters, numbers, spaces, hyphens, underscores, and periods")]
    string Name,

    [property: StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    string? Description
);

public record UpdateRoleRequest(
    [property: Required(ErrorMessage = "Role name is required")]
    [property: StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters")]
    [property: AllowedCharacters(@"^[a-zA-Z0-9\s\-_\.]+$", ErrorMessage = "Role name can only contain letters, numbers, spaces, hyphens, underscores, and periods")]
    string Name,

    [property: StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    string? Description
);

public record RoleDto(Guid Id, string Name, string? Description);

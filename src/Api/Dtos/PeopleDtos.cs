using System.ComponentModel.DataAnnotations;
using App.Validation;

namespace Api.Dtos;

public record CreatePersonRequest(
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
    [FullNameFormat(ErrorMessage = "Full name contains invalid characters")]
    string FullName,

    [PhoneFormat(ErrorMessage = "Phone number must be a valid format")]
    string? Phone,

    [NoEmptyGuids(ErrorMessage = "All role IDs must be valid non-empty GUIDs")]
    IEnumerable<Guid>? RoleIds
);

public record UpdatePersonRequest(
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
    [FullNameFormat(ErrorMessage = "Full name contains invalid characters")]
    string FullName,

    [PhoneFormat(ErrorMessage = "Phone number must be a valid format")]
    string? Phone,

    [NoEmptyGuids(ErrorMessage = "All role IDs must be valid non-empty GUIDs")]
    IEnumerable<Guid>? RoleIds,

    byte[]? RowVersion
);

public record PersonResponse(Guid Id, string FullName, string? Phone, IEnumerable<RoleResponse> Roles, byte[]? RowVersion);

public record RoleResponse(Guid Id, string Name, string? Description);

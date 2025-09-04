using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
            .WithMessage("Role name can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Role name is required")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
            .WithMessage("Role name can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

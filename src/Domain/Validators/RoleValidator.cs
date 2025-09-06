using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class RoleValidator : AbstractValidator<Role>
{
    public RoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Role name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID cannot be empty");
    }
}

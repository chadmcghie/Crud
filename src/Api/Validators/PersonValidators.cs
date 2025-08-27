using FluentValidation;
using Api.Dtos;

namespace Api.Validators;

public class CreatePersonRequestValidator : AbstractValidator<CreatePersonRequest>
{
    public CreatePersonRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required")
            .MaximumLength(200)
            .WithMessage("Full name cannot exceed 200 characters");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)\.]{7,15}$")
            .WithMessage("Phone number must be a valid format")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.RoleIds)
            .Must(roleIds => roleIds?.All(id => id != Guid.Empty) ?? true)
            .WithMessage("All role IDs must be valid non-empty GUIDs")
            .When(x => x.RoleIds != null);
    }
}

public class UpdatePersonRequestValidator : AbstractValidator<UpdatePersonRequest>
{
    public UpdatePersonRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required")
            .MaximumLength(200)
            .WithMessage("Full name cannot exceed 200 characters");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)\.]{7,15}$")
            .WithMessage("Phone number must be a valid format")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.RoleIds)
            .Must(roleIds => roleIds?.All(id => id != Guid.Empty) ?? true)
            .WithMessage("All role IDs must be valid non-empty GUIDs")
            .When(x => x.RoleIds != null);
    }
}
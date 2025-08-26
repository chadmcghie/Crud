using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public class CreatePersonRequestValidator : AbstractValidator<CreatePersonRequest>
{
    public CreatePersonRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.");

        RuleFor(x => x.Phone)
            .Matches(@"^[\+]?[1-9][\d]{0,15}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number must be a valid format.");

        RuleFor(x => x.RoleIds)
            .NotNull()
            .WithMessage("Role IDs cannot be null.")
            .Must(roleIds => roleIds == null || roleIds.All(id => id != Guid.Empty))
            .WithMessage("Role IDs must be valid GUIDs.");
    }
}

public class UpdatePersonRequestValidator : AbstractValidator<UpdatePersonRequest>
{
    public UpdatePersonRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.");

        RuleFor(x => x.Phone)
            .Matches(@"^[\+]?[1-9][\d]{0,15}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number must be a valid format.");

        RuleFor(x => x.RoleIds)
            .NotNull()
            .WithMessage("Role IDs cannot be null.")
            .Must(roleIds => roleIds == null || roleIds.All(id => id != Guid.Empty))
            .WithMessage("Role IDs must be valid GUIDs.");
    }
}
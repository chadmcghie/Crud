using FluentValidation;

namespace App.Features.People;

public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Full name contains invalid characters");

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

public class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Person ID is required");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Full name contains invalid characters");

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

public class DeletePersonCommandValidator : AbstractValidator<DeletePersonCommand>
{
    public DeletePersonCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Person ID is required");
    }
}

public class GetPersonQueryValidator : AbstractValidator<GetPersonQuery>
{
    public GetPersonQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Person ID is required");
    }
}
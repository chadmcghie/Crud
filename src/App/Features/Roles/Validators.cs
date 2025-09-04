using FluentValidation;

namespace App.Features.Roles;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Role name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Role name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required");
    }
}

public class GetRoleQueryValidator : AbstractValidator<GetRoleQuery>
{
    public GetRoleQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required");
    }
}

using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class WallValidator : AbstractValidator<Wall>
{
    public WallValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wall name is required")
            .MaximumLength(200).WithMessage("Wall name cannot exceed 200 characters");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Height cannot exceed 100 feet");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Length must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Length cannot exceed 1000 feet");

        RuleFor(x => x.Thickness)
            .GreaterThan(0).WithMessage("Thickness must be greater than 0")
            .LessThanOrEqualTo(48).WithMessage("Thickness cannot exceed 48 inches");

        RuleFor(x => x.AssemblyType)
            .NotEmpty().WithMessage("Assembly type is required")
            .MaximumLength(500).WithMessage("Assembly type cannot exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Wall ID cannot be empty");
    }
}

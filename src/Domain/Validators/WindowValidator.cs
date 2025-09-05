using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class WindowValidator : AbstractValidator<Window>
{
    public WindowValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Window name is required")
            .MaximumLength(200).WithMessage("Window name cannot exceed 200 characters");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0")
            .LessThanOrEqualTo(20).WithMessage("Width cannot exceed 20 feet");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0")
            .LessThanOrEqualTo(20).WithMessage("Height cannot exceed 20 feet");

        RuleFor(x => x.Area)
            .GreaterThan(0).WithMessage("Area must be greater than 0")
            .LessThanOrEqualTo(400).WithMessage("Area cannot exceed 400 square feet");

        RuleFor(x => x.FrameType)
            .NotEmpty().WithMessage("Frame type is required")
            .MaximumLength(100).WithMessage("Frame type cannot exceed 100 characters");

        RuleFor(x => x.GlazingType)
            .NotEmpty().WithMessage("Glazing type is required")
            .MaximumLength(100).WithMessage("Glazing type cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Window ID cannot be empty");
    }
}

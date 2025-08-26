using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public class CreateWallRequestValidator : AbstractValidator<CreateWallRequest>
{
    public CreateWallRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Wall name is required.")
            .MaximumLength(200)
            .WithMessage("Wall name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Length)
            .InclusiveBetween(0.1, 1000)
            .WithMessage("Length must be between 0.1 and 1000 feet.");

        RuleFor(x => x.Height)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Height must be between 0.1 and 100 feet.");

        RuleFor(x => x.Thickness)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Thickness must be between 0.1 and 100 inches.");

        RuleFor(x => x.AssemblyType)
            .NotEmpty()
            .WithMessage("Assembly type is required.")
            .MaximumLength(500)
            .WithMessage("Assembly type cannot exceed 500 characters.");

        RuleFor(x => x.AssemblyDetails)
            .MaximumLength(1000)
            .WithMessage("Assembly details cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AssemblyDetails));

        RuleFor(x => x.RValue)
            .InclusiveBetween(0, 100)
            .WithMessage("R-Value must be between 0 and 100.")
            .When(x => x.RValue.HasValue);

        RuleFor(x => x.UValue)
            .InclusiveBetween(0, 10)
            .WithMessage("U-Value must be between 0 and 10.")
            .When(x => x.UValue.HasValue);

        RuleFor(x => x.MaterialLayers)
            .MaximumLength(2000)
            .WithMessage("Material layers cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.MaterialLayers));

        RuleFor(x => x.Orientation)
            .MaximumLength(50)
            .WithMessage("Orientation cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Orientation));

        RuleFor(x => x.Location)
            .MaximumLength(50)
            .WithMessage("Location cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));
    }
}

public class UpdateWallRequestValidator : AbstractValidator<UpdateWallRequest>
{
    public UpdateWallRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Wall name is required.")
            .MaximumLength(200)
            .WithMessage("Wall name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Length)
            .InclusiveBetween(0.1, 1000)
            .WithMessage("Length must be between 0.1 and 1000 feet.");

        RuleFor(x => x.Height)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Height must be between 0.1 and 100 feet.");

        RuleFor(x => x.Thickness)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Thickness must be between 0.1 and 100 inches.");

        RuleFor(x => x.AssemblyType)
            .NotEmpty()
            .WithMessage("Assembly type is required.")
            .MaximumLength(500)
            .WithMessage("Assembly type cannot exceed 500 characters.");

        RuleFor(x => x.AssemblyDetails)
            .MaximumLength(1000)
            .WithMessage("Assembly details cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AssemblyDetails));

        RuleFor(x => x.RValue)
            .InclusiveBetween(0, 100)
            .WithMessage("R-Value must be between 0 and 100.")
            .When(x => x.RValue.HasValue);

        RuleFor(x => x.UValue)
            .InclusiveBetween(0, 10)
            .WithMessage("U-Value must be between 0 and 10.")
            .When(x => x.UValue.HasValue);

        RuleFor(x => x.MaterialLayers)
            .MaximumLength(2000)
            .WithMessage("Material layers cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.MaterialLayers));

        RuleFor(x => x.Orientation)
            .MaximumLength(50)
            .WithMessage("Orientation cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Orientation));

        RuleFor(x => x.Location)
            .MaximumLength(50)
            .WithMessage("Location cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));
    }
}
using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public class CreateWindowRequestValidator : AbstractValidator<CreateWindowRequest>
{
    public CreateWindowRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Window name is required.")
            .MaximumLength(200)
            .WithMessage("Window name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Width)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Width must be between 0.1 and 100 feet.");

        RuleFor(x => x.Height)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Height must be between 0.1 and 100 feet.");

        RuleFor(x => x.Area)
            .InclusiveBetween(0.01, 1000)
            .WithMessage("Area must be between 0.01 and 1000 square feet.");

        RuleFor(x => x.FrameType)
            .NotEmpty()
            .WithMessage("Frame type is required.")
            .MaximumLength(100)
            .WithMessage("Frame type cannot exceed 100 characters.");

        RuleFor(x => x.FrameDetails)
            .MaximumLength(500)
            .WithMessage("Frame details cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.FrameDetails));

        RuleFor(x => x.GlazingType)
            .NotEmpty()
            .WithMessage("Glazing type is required.")
            .MaximumLength(100)
            .WithMessage("Glazing type cannot exceed 100 characters.");

        RuleFor(x => x.GlazingDetails)
            .MaximumLength(500)
            .WithMessage("Glazing details cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.GlazingDetails));

        RuleFor(x => x.UValue)
            .InclusiveBetween(0, 10)
            .WithMessage("U-Value must be between 0 and 10.")
            .When(x => x.UValue.HasValue);

        RuleFor(x => x.SolarHeatGainCoefficient)
            .InclusiveBetween(0, 1)
            .WithMessage("Solar Heat Gain Coefficient must be between 0 and 1.")
            .When(x => x.SolarHeatGainCoefficient.HasValue);

        RuleFor(x => x.VisibleTransmittance)
            .InclusiveBetween(0, 1)
            .WithMessage("Visible Transmittance must be between 0 and 1.")
            .When(x => x.VisibleTransmittance.HasValue);

        RuleFor(x => x.AirLeakage)
            .InclusiveBetween(0, 100)
            .WithMessage("Air Leakage must be between 0 and 100.")
            .When(x => x.AirLeakage.HasValue);

        RuleFor(x => x.EnergyStarRating)
            .MaximumLength(50)
            .WithMessage("Energy Star Rating cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.EnergyStarRating));

        RuleFor(x => x.NFRCRating)
            .MaximumLength(50)
            .WithMessage("NFRC Rating cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.NFRCRating));

        RuleFor(x => x.Orientation)
            .MaximumLength(50)
            .WithMessage("Orientation cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Orientation));

        RuleFor(x => x.Location)
            .MaximumLength(100)
            .WithMessage("Location cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.InstallationType)
            .MaximumLength(50)
            .WithMessage("Installation Type cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.InstallationType));

        RuleFor(x => x.OperationType)
            .MaximumLength(100)
            .WithMessage("Operation Type cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.OperationType));
    }
}

public class UpdateWindowRequestValidator : AbstractValidator<UpdateWindowRequest>
{
    public UpdateWindowRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Window name is required.")
            .MaximumLength(200)
            .WithMessage("Window name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Width)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Width must be between 0.1 and 100 feet.");

        RuleFor(x => x.Height)
            .InclusiveBetween(0.1, 100)
            .WithMessage("Height must be between 0.1 and 100 feet.");

        RuleFor(x => x.Area)
            .InclusiveBetween(0.01, 1000)
            .WithMessage("Area must be between 0.01 and 1000 square feet.");

        RuleFor(x => x.FrameType)
            .NotEmpty()
            .WithMessage("Frame type is required.")
            .MaximumLength(100)
            .WithMessage("Frame type cannot exceed 100 characters.");

        RuleFor(x => x.FrameDetails)
            .MaximumLength(500)
            .WithMessage("Frame details cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.FrameDetails));

        RuleFor(x => x.GlazingType)
            .NotEmpty()
            .WithMessage("Glazing type is required.")
            .MaximumLength(100)
            .WithMessage("Glazing type cannot exceed 100 characters.");

        RuleFor(x => x.GlazingDetails)
            .MaximumLength(500)
            .WithMessage("Glazing details cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.GlazingDetails));

        RuleFor(x => x.UValue)
            .InclusiveBetween(0, 10)
            .WithMessage("U-Value must be between 0 and 10.")
            .When(x => x.UValue.HasValue);

        RuleFor(x => x.SolarHeatGainCoefficient)
            .InclusiveBetween(0, 1)
            .WithMessage("Solar Heat Gain Coefficient must be between 0 and 1.")
            .When(x => x.SolarHeatGainCoefficient.HasValue);

        RuleFor(x => x.VisibleTransmittance)
            .InclusiveBetween(0, 1)
            .WithMessage("Visible Transmittance must be between 0 and 1.")
            .When(x => x.VisibleTransmittance.HasValue);

        RuleFor(x => x.AirLeakage)
            .InclusiveBetween(0, 100)
            .WithMessage("Air Leakage must be between 0 and 100.")
            .When(x => x.AirLeakage.HasValue);

        RuleFor(x => x.EnergyStarRating)
            .MaximumLength(50)
            .WithMessage("Energy Star Rating cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.EnergyStarRating));

        RuleFor(x => x.NFRCRating)
            .MaximumLength(50)
            .WithMessage("NFRC Rating cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.NFRCRating));

        RuleFor(x => x.Orientation)
            .MaximumLength(50)
            .WithMessage("Orientation cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Orientation));

        RuleFor(x => x.Location)
            .MaximumLength(100)
            .WithMessage("Location cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.InstallationType)
            .MaximumLength(50)
            .WithMessage("Installation Type cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.InstallationType));

        RuleFor(x => x.OperationType)
            .MaximumLength(100)
            .WithMessage("Operation Type cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.OperationType));
    }
}
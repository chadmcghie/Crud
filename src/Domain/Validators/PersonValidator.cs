using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Full name contains invalid characters");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)\.]{7,15}$")
            .WithMessage("Phone number must be a valid format")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Person ID cannot be empty");
    }
}

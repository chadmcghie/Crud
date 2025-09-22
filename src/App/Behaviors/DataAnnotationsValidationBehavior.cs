using System.ComponentModel.DataAnnotations;
using App.Validation;
using MediatR;

namespace App.Behaviors;

public class DataAnnotationsValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Perform DataAnnotations validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        if (!isValid)
        {
            var errors = validationResults
                .Select(vr => new ValidationFailure(
                    vr.MemberNames.FirstOrDefault() ?? "Object",
                    vr.ErrorMessage ?? "Validation failed"))
                .ToList();

            throw new App.Validation.ValidationException(errors);
        }

        return await next();
    }
}

using System.ComponentModel.DataAnnotations;
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
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        
        bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);
        
        if (!isValid)
        {
            var errors = validationResults
                .Select(vr => new FluentValidation.Results.ValidationFailure(
                    vr.MemberNames.FirstOrDefault() ?? "Object", 
                    vr.ErrorMessage ?? "Validation failed"))
                .ToList();
            
            throw new FluentValidation.ValidationException(errors);
        }
        
        return await next();
    }
}
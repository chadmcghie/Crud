using System.ComponentModel.DataAnnotations;

namespace App.Validation;

/// <summary>
/// Custom validation exception to replace FluentValidation.ValidationException
/// </summary>
public class ValidationException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<ValidationFailure>();
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new List<ValidationFailure>();
    }
}

/// <summary>
/// Validation failure details
/// </summary>
public class ValidationFailure
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }

    public ValidationFailure(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }

    public ValidationFailure(string propertyName, string errorMessage, object? attemptedValue)
        : this(propertyName, errorMessage)
    {
        AttemptedValue = attemptedValue;
    }
}
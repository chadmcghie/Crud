using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace App.Validation;

/// <summary>
/// Validates that a string only contains allowed characters based on a regex pattern
/// </summary>
public class AllowedCharactersAttribute : ValidationAttribute
{
    private readonly string _pattern;
    private readonly Regex _regex;

    public AllowedCharactersAttribute(string pattern)
    {
        _pattern = pattern;
        _regex = new Regex(pattern, RegexOptions.Compiled);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // Let Required attribute handle empty values
        }

        var stringValue = value.ToString()!;

        if (!_regex.IsMatch(stringValue))
        {
            return new ValidationResult(
                ErrorMessage ?? $"Field contains invalid characters. Only characters matching pattern {_pattern} are allowed.",
                new[] { validationContext.MemberName ?? "Object" });
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that a collection of GUIDs doesn't contain empty GUIDs
/// </summary>
public class NoEmptyGuidsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // null is acceptable
        }

        if (value is IEnumerable<Guid> guids)
        {
            if (guids.Any(g => g == Guid.Empty))
            {
                return new ValidationResult(
                    ErrorMessage ?? "All GUIDs must be valid non-empty values.",
                    new[] { validationContext.MemberName ?? "Object" });
            }
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates phone number format when not null or empty
/// </summary>
public class PhoneFormatAttribute : ValidationAttribute
{
    private static readonly Regex PhoneRegex = new(@"^\+?[\d\s\-\(\)\.]{7,15}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // Allow empty/null values
        }

        var phoneValue = value.ToString()!;

        if (!PhoneRegex.IsMatch(phoneValue))
        {
            return new ValidationResult(
                ErrorMessage ?? "Phone number must be a valid format.",
                new[] { validationContext.MemberName ?? "Object" });
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates full name format
/// </summary>
public class FullNameFormatAttribute : ValidationAttribute
{
    private static readonly Regex NameRegex = new(@"^[a-zA-Z\s\-'\.]+$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // Let Required attribute handle empty values
        }

        var nameValue = value.ToString()!;

        if (!NameRegex.IsMatch(nameValue))
        {
            return new ValidationResult(
                ErrorMessage ?? "Full name contains invalid characters.",
                new[] { validationContext.MemberName ?? "Object" });
        }

        return ValidationResult.Success;
    }
}

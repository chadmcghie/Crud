using System.ComponentModel.DataAnnotations;
using Api.Dtos;
using FluentAssertions;

namespace Tests.Unit.Backend.Validators;

public class DebugValidationTests
{
    [Fact]
    public void Debug_ValidationResults()
    {
        // Arrange
        var request = new CreateRoleRequest("Admin@!", null);

        // Act
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // Assert
        Console.WriteLine($"Is Valid: {isValid}");
        Console.WriteLine($"Validation Results Count: {validationResults.Count}");
        
        foreach (var result in validationResults)
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            Console.WriteLine($"Members: {string.Join(", ", result.MemberNames)}");
        }
        
        // Debug: Check properties
        var properties = request.GetType().GetProperties();
        foreach (var prop in properties)
        {
            Console.WriteLine($"Property: {prop.Name}, Value: {prop.GetValue(request)}");
            
            // Check attributes
            var attributes = prop.GetCustomAttributes(typeof(ValidationAttribute), true);
            Console.WriteLine($"  Attributes count: {attributes.Length}");
            foreach (ValidationAttribute attr in attributes)
            {
                Console.WriteLine($"  Attribute: {attr.GetType().Name}");
            }
        }
    }
}
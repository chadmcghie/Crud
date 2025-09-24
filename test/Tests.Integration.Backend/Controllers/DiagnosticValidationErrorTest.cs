using System.Net;
using System.Text.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Controllers;

/// <summary>
/// Diagnostic test to capture exact validation error messages from POST /api/people endpoint
/// This test replicates the exact scenario from MultiProviderIntegrationTestBase to debug HTTP 400 BadRequest errors
/// </summary>
public class DiagnosticValidationErrorTest
{
    private readonly ITestOutputHelper _output;

    public DiagnosticValidationErrorTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task POST_People_Should_Capture_Validation_Error_Details()
    {
        // Using SQLite provider like the failing tests
        using var testInstance = new TestProviderInstance(DatabaseProvider.SQLite);
        _output.WriteLine($"Testing with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Replicate exact same data creation as failing test
            var personName = testInstance.CreateProviderSpecificTestData("John Doe");
            var createRequest = TestDataBuilders.CreatePersonRequest(personName, "123-456-7890");

            _output.WriteLine($"Generated person name: '{personName}'");
            _output.WriteLine($"Person name length: {personName.Length}");
            _output.WriteLine($"CreatePersonRequest: FullName='{createRequest.FullName}', Phone='{createRequest.Phone}'");

            // Act - Make the POST request that's failing
            var response = await testInstance.AuthenticatedPostJsonAsync("/api/people", createRequest);

            // Capture full response details
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response Status Code: {response.StatusCode}");
            _output.WriteLine($"Response Content: {responseContent}");
            _output.WriteLine($"Response Headers: {response.Headers}");

            // If it's a 400 BadRequest, parse and display validation errors
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                _output.WriteLine("=== VALIDATION ERROR DETAILS ===");

                try
                {
                    // Try to parse as validation problem details
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var problemDetails = JsonSerializer.Deserialize<JsonElement>(responseContent, options);

                    if (problemDetails.TryGetProperty("errors", out var errorsElement))
                    {
                        _output.WriteLine("Validation Errors:");
                        foreach (var error in errorsElement.EnumerateObject())
                        {
                            _output.WriteLine($"  Field: {error.Name}");
                            if (error.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var message in error.Value.EnumerateArray())
                                {
                                    _output.WriteLine($"    - {message.GetString()}");
                                }
                            }
                            else
                            {
                                _output.WriteLine($"    - {error.Value.GetString()}");
                            }
                        }
                    }

                    if (problemDetails.TryGetProperty("title", out var titleElement))
                    {
                        _output.WriteLine($"Title: {titleElement.GetString()}");
                    }

                    if (problemDetails.TryGetProperty("detail", out var detailElement))
                    {
                        _output.WriteLine($"Detail: {detailElement.GetString()}");
                    }
                }
                catch (JsonException ex)
                {
                    _output.WriteLine($"Could not parse response as JSON: {ex.Message}");
                }

                // Also try parsing the raw response content
                _output.WriteLine($"Raw Response Content: {responseContent}");
            }

            // Don't assert success - we want to see the error details
            // Just log what we got for analysis
            _output.WriteLine($"Final Status: {response.StatusCode}");
        });
    }

    /// <summary>
    /// Simple wrapper to instantiate the MultiProviderIntegrationTestBase for SQLite provider
    /// </summary>
    private class TestProviderInstance : MultiProviderIntegrationTestBase
    {
        public TestProviderInstance(DatabaseProvider provider) : base(provider) { }
    }
}
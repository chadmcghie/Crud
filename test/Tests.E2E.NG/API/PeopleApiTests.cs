using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.API;

/// <summary>
/// C# wrapper tests for People API tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class PeopleApiTests
{
    private readonly ITestOutputHelper _output;

    public PeopleApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetPeople_ShouldReturnEmptyArrayWhenNoPeopleExist()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "GET /api/people - should return empty array when no people exist");
    }

    [Fact]
    public async Task PostPeople_ShouldCreateANewPersonSuccessfully()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "POST /api/people - should create a new person successfully");
    }

    [Fact]
    public async Task PostPeople_ShouldCreatePersonWithOnlyRequiredFields()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "POST /api/people - should create person with only required fields");
    }

    [Fact]
    public async Task PostPeople_ShouldCreatePersonWithRoles()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "POST /api/people - should create person with roles");
    }

    [Fact]
    public async Task PostPeople_ShouldValidateRequiredFields()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "POST /api/people - should validate required fields");
    }

    [Fact]
    public async Task GetPersonById_ShouldReturnSpecificPerson()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "GET /api/people/{id} - should return specific person");
    }

    [Fact]
    public async Task GetPersonById_ShouldReturn404ForNonExistentPerson()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "GET /api/people/{id} - should return 404 for non-existent person");
    }

    [Fact]
    public async Task PutPersonById_ShouldUpdateExistingPerson()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "PUT /api/people/{id} - should update existing person");
    }

    [Fact]
    public async Task PutPersonById_ShouldUpdatePersonRoles()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "PUT /api/people/{id} - should update person roles");
    }

    [Fact]
    public async Task PutPersonById_ShouldReturn404ForNonExistentPerson()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "PUT /api/people/{id} - should return 404 for non-existent person");
    }

    [Fact]
    public async Task DeletePersonById_ShouldDeleteExistingPerson()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "DELETE /api/people/{id} - should delete existing person");
    }

    [Fact]
    public async Task DeletePersonById_ShouldReturn404ForNonExistentPerson()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "DELETE /api/people/{id} - should return 404 for non-existent person");
    }

    [Fact]
    public async Task ShouldHandleMultiplePeopleCorrectly()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should handle multiple people correctly");
    }

    [Fact]
    public async Task ShouldHandleInvalidRoleIdsGracefully()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should handle invalid role IDs gracefully");
    }

    [Fact]
    public async Task ShouldMaintainReferentialIntegrityWithRoles()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should maintain referential integrity with roles");
    }

    [Fact]
    public async Task ShouldHandleSpecialCharactersInPersonData()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should handle special characters in person data");
    }

    [Fact]
    public async Task ShouldHandlePhoneNumberFormats()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should handle phone number formats");
    }

    [Fact]
    public async Task ShouldReturnProperHttpStatusCodes()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should return proper HTTP status codes");
    }

    [Fact]
    public async Task ShouldHandleConcurrentOperationsCorrectly()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should handle concurrent operations correctly");
    }

    [Fact]
    public async Task ShouldHandleRoleAssignmentEdgeCases()
    {
        await RunPlaywrightTest("tests/api/people-api.spec.ts", "should handle role assignment edge cases");
    }

    private async Task RunPlaywrightTest(string specFile, string testName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "npx",
            Arguments = $"playwright test \"{specFile}\" --grep \"{testName}\"",
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        _output.WriteLine($"Playwright Output: {output}");
        if (!string.IsNullOrEmpty(error))
        {
            _output.WriteLine($"Playwright Error: {error}");
        }

        Assert.True(process.ExitCode == 0, $"Playwright test failed. Exit code: {process.ExitCode}. Error: {error}");
    }
}
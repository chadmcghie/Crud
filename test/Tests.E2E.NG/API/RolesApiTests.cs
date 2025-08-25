using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.API;

/// <summary>
/// C# wrapper tests for Roles API tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class RolesApiTests
{
    private readonly ITestOutputHelper _output;

    public RolesApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetRoles_ShouldReturnEmptyArrayWhenNoRolesExist()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "GET /api/roles - should return empty array when no roles exist");
    }

    [Fact]
    public async Task PostRoles_ShouldCreateANewRoleSuccessfully()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "POST /api/roles - should create a new role successfully");
    }

    [Fact]
    public async Task PostRoles_ShouldCreateRoleWithOnlyRequiredFields()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "POST /api/roles - should create role with only required fields");
    }

    [Fact]
    public async Task PostRoles_ShouldValidateRequiredFields()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "POST /api/roles - should validate required fields");
    }

    [Fact]
    public async Task GetRoleById_ShouldReturnSpecificRole()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "GET /api/roles/{id} - should return specific role");
    }

    [Fact]
    public async Task GetRoleById_ShouldReturn404ForNonExistentRole()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "GET /api/roles/{id} - should return 404 for non-existent role");
    }

    [Fact]
    public async Task PutRoleById_ShouldUpdateExistingRole()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "PUT /api/roles/{id} - should update existing role");
    }

    [Fact]
    public async Task PutRoleById_ShouldReturn404ForNonExistentRole()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "PUT /api/roles/{id} - should return 404 for non-existent role");
    }

    [Fact]
    public async Task DeleteRoleById_ShouldDeleteExistingRole()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "DELETE /api/roles/{id} - should delete existing role");
    }

    [Fact]
    public async Task DeleteRoleById_ShouldReturn404ForNonExistentRole()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "DELETE /api/roles/{id} - should return 404 for non-existent role");
    }

    [Fact]
    public async Task ShouldHandleMultipleRolesCorrectly()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should handle multiple roles correctly");
    }

    [Fact]
    public async Task ShouldMaintainDataIntegrityDuringConcurrentOperations()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should maintain data integrity during concurrent operations");
    }

    [Fact]
    public async Task ShouldHandleRoleNameUniqueness()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should handle role name uniqueness");
    }

    [Fact]
    public async Task ShouldHandleSpecialCharactersInRoleData()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should handle special characters in role data");
    }

    [Fact]
    public async Task ShouldHandleLargeDescriptionText()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should handle large description text");
    }

    [Fact]
    public async Task ShouldReturnProperHttpStatusCodes()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should return proper HTTP status codes");
    }

    [Fact]
    public async Task ShouldHandleMalformedJsonRequests()
    {
        await RunPlaywrightTest("tests/api/roles-api.spec.ts", "should handle malformed JSON requests");
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
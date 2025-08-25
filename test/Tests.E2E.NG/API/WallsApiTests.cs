using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.API;

/// <summary>
/// C# wrapper tests for Walls API tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class WallsApiTests
{
    private readonly ITestOutputHelper _output;

    public WallsApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetWalls_ShouldReturnEmptyArrayWhenNoWallsExist()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "GET /api/walls - should return empty array when no walls exist");
    }

    [Fact]
    public async Task PostWalls_ShouldCreateANewWallSuccessfully()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "POST /api/walls - should create a new wall successfully");
    }

    [Fact]
    public async Task PostWalls_ShouldCreateWallWithOnlyRequiredFields()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "POST /api/walls - should create wall with only required fields");
    }

    [Fact]
    public async Task PostWalls_ShouldValidateRequiredFields()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "POST /api/walls - should validate required fields");
    }

    [Fact]
    public async Task PostWalls_ShouldValidateNumericFields()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "POST /api/walls - should validate numeric fields");
    }

    [Fact]
    public async Task GetWallById_ShouldReturnSpecificWall()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "GET /api/walls/{id} - should return specific wall");
    }

    [Fact]
    public async Task GetWallById_ShouldReturn404ForNonExistentWall()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "GET /api/walls/{id} - should return 404 for non-existent wall");
    }

    [Fact]
    public async Task PutWallById_ShouldUpdateExistingWall()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "PUT /api/walls/{id} - should update existing wall");
    }

    [Fact]
    public async Task PutWallById_ShouldReturn404ForNonExistentWall()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "PUT /api/walls/{id} - should return 404 for non-existent wall");
    }

    [Fact]
    public async Task DeleteWallById_ShouldDeleteExistingWall()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "DELETE /api/walls/{id} - should delete existing wall");
    }

    [Fact]
    public async Task DeleteWallById_ShouldReturn404ForNonExistentWall()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "DELETE /api/walls/{id} - should return 404 for non-existent wall");
    }

    [Fact]
    public async Task ShouldHandleMultipleWallsCorrectly()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle multiple walls correctly");
    }

    [Fact]
    public async Task ShouldHandleDecimalPrecisionCorrectly()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle decimal precision correctly");
    }

    [Fact]
    public async Task ShouldHandleAssemblyTypesCorrectly()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle assembly types correctly");
    }

    [Fact]
    public async Task ShouldHandleOrientationValuesCorrectly()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle orientation values correctly");
    }

    [Fact]
    public async Task ShouldHandleSpecialCharactersInWallData()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle special characters in wall data");
    }

    [Fact]
    public async Task ShouldMaintainTimestampIntegrity()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should maintain timestamp integrity");
    }

    [Fact]
    public async Task ShouldReturnProperHttpStatusCodes()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should return proper HTTP status codes");
    }

    [Fact]
    public async Task ShouldHandleConcurrentOperationsCorrectly()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle concurrent operations correctly");
    }

    [Fact]
    public async Task ShouldHandleLargeTextFields()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle large text fields");
    }

    [Fact]
    public async Task ShouldHandleBoundaryValuesForNumericFields()
    {
        await RunPlaywrightTest("tests/api/walls-api.spec.ts", "should handle boundary values for numeric fields");
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
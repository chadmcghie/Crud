using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.Integration;

/// <summary>
/// C# wrapper tests for Full Workflow Integration tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class FullWorkflowTests
{
    private readonly ITestOutputHelper _output;

    public FullWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ShouldCompleteFullRoleAndPersonManagementWorkflow()
    {
        await RunPlaywrightTest("tests/integration/full-workflow.spec.ts", "should complete full role and person management workflow");
    }

    [Fact]
    public async Task ShouldHandleMixedUIAndAPIOperations()
    {
        await RunPlaywrightTest("tests/integration/full-workflow.spec.ts", "should handle mixed UI and API operations");
    }

    [Fact]
    public async Task ShouldMaintainDataIntegrityDuringRapidOperations()
    {
        await RunPlaywrightTest("tests/integration/full-workflow.spec.ts", "should maintain data integrity during rapid operations");
    }

    [Fact]
    public async Task ShouldHandleErrorScenariosGracefully()
    {
        await RunPlaywrightTest("tests/integration/full-workflow.spec.ts", "should handle error scenarios gracefully");
    }

    [Fact]
    public async Task ShouldPreserveStateDuringTabSwitching()
    {
        await RunPlaywrightTest("tests/integration/full-workflow.spec.ts", "should preserve state during tab switching");
    }

    [Fact]
    public async Task ShouldHandleBrowserRefreshCorrectly()
    {
        await RunPlaywrightTest("tests/integration/full-workflow.spec.ts", "should handle browser refresh correctly");
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
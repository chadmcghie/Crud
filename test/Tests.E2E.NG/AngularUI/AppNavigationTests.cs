using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.AngularUI;

/// <summary>
/// C# wrapper tests for Angular UI Application Navigation and Layout tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class AppNavigationTests
{
    private readonly ITestOutputHelper _output;

    public AppNavigationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ShouldLoadTheApplicationSuccessfully()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should load the application successfully");
    }

    [Fact]
    public async Task ShouldHavePeopleTabActiveByDefault()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should have people tab active by default");
    }

    [Fact]
    public async Task ShouldSwitchBetweenTabsCorrectly()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should switch between tabs correctly");
    }

    [Fact]
    public async Task ShouldResetFormsWhenSwitchingTabs()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should reset forms when switching tabs");
    }

    [Fact]
    public async Task ShouldMaintainResponsiveDesign()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should maintain responsive design");
    }

    [Fact]
    public async Task ShouldDisplayCorrectTabIndicators()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should display correct tab indicators");
    }

    [Fact]
    public async Task ShouldHandlePageRefreshCorrectly()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should handle page refresh correctly");
    }

    [Fact]
    public async Task ShouldDisplayProperStylingAndLayout()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should display proper styling and layout");
    }

    [Fact]
    public async Task ShouldHandleKeyboardNavigation()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should handle keyboard navigation");
    }

    [Fact]
    public async Task ShouldDisplayCorrectContentSections()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should display correct content sections");
    }

    [Fact]
    public async Task ShouldHandleFormSectionVisibility()
    {
        await RunPlaywrightTest("tests/angular-ui/app-navigation.spec.ts", "should handle form section visibility");
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
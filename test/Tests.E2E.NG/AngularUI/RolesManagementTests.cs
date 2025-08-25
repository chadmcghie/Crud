using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.AngularUI;

/// <summary>
/// C# wrapper tests for Angular UI Roles Management tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class RolesManagementTests
{
    private readonly ITestOutputHelper _output;

    public RolesManagementTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ShouldDisplayEmptyStateWhenNoRolesExist()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should display empty state when no roles exist");
    }

    [Fact]
    public async Task ShouldCreateANewRoleSuccessfully()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should create a new role successfully");
    }

    [Fact]
    public async Task ShouldCreateMultipleRoles()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should create multiple roles");
    }

    [Fact]
    public async Task ShouldValidateRequiredFields()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should validate required fields");
    }

    [Fact]
    public async Task ShouldEditAnExistingRole()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should edit an existing role");
    }

    [Fact]
    public async Task ShouldDeleteARole()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should delete a role");
    }

    [Fact]
    public async Task ShouldHandleRoleCreationWithOnlyRequiredFields()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should handle role creation with only required fields");
    }

    [Fact]
    public async Task ShouldRefreshTheRolesList()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should refresh the roles list");
    }

    [Fact]
    public async Task ShouldHandleFormCancellation()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should handle form cancellation");
    }

    [Fact]
    public async Task ShouldHandleFormReset()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should handle form reset");
    }

    [Fact]
    public async Task ShouldMaintainDataIntegrityAcrossTabSwitches()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should maintain data integrity across tab switches");
    }

    [Fact]
    public async Task ShouldDisplayRoleInformationCorrectlyInTable()
    {
        await RunPlaywrightTest("tests/angular-ui/roles.spec.ts", "should display role information correctly in table");
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
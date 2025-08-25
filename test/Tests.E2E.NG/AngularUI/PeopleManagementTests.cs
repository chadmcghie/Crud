using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.E2E.NG.AngularUI;

/// <summary>
/// C# wrapper tests for Angular UI People Management tests.
/// These tests execute the corresponding Playwright TypeScript tests and report results to Visual Studio Test Explorer.
/// </summary>
public class PeopleManagementTests
{
    private readonly ITestOutputHelper _output;

    public PeopleManagementTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ShouldDisplayEmptyStateWhenNoPeopleExist()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should display empty state when no people exist");
    }

    [Fact]
    public async Task ShouldCreateANewPersonSuccessfully()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should create a new person successfully");
    }

    [Fact]
    public async Task ShouldCreateMultiplePeople()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should create multiple people");
    }

    [Fact]
    public async Task ShouldValidateRequiredFields()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should validate required fields");
    }

    [Fact]
    public async Task ShouldCreatePersonWithRoles()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should create person with roles");
    }

    [Fact]
    public async Task ShouldEditAnExistingPerson()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should edit an existing person");
    }

    [Fact]
    public async Task ShouldDeleteAPerson()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should delete a person");
    }

    [Fact]
    public async Task ShouldHandlePersonCreationWithOnlyRequiredFields()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should handle person creation with only required fields");
    }

    [Fact]
    public async Task ShouldRefreshThePeopleList()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should refresh the people list");
    }

    [Fact]
    public async Task ShouldHandleFormCancellation()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should handle form cancellation");
    }

    [Fact]
    public async Task ShouldHandleFormReset()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should handle form reset");
    }

    [Fact]
    public async Task ShouldDisplayPersonInformationCorrectlyInTable()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should display person information correctly in table");
    }

    [Fact]
    public async Task ShouldShowNoRolesAssignedWhenPersonHasNoRoles()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should show no roles assigned when person has no roles");
    }

    [Fact]
    public async Task ShouldHandleRoleAssignmentAndRemoval()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should handle role assignment and removal");
    }

    [Fact]
    public async Task ShouldMaintainDataIntegrityAcrossTabSwitches()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should maintain data integrity across tab switches");
    }

    [Fact]
    public async Task ShouldShowMessageWhenNoRolesAreAvailable()
    {
        await RunPlaywrightTest("tests/angular-ui/people.spec.ts", "should show message when no roles are available");
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
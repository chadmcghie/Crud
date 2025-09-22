using System.Net;
using Api.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.Controllers.MultiProvider;

/// <summary>
/// Multi-provider integration tests for PeopleController
/// Verifies that people management with relationships works consistently across all database providers
/// </summary>
public class PeopleControllerMultiProviderTests : MultiProviderIntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public PeopleControllerMultiProviderTests(DatabaseProvider provider, ITestOutputHelper output)
        : base(provider)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task GET_People_Should_Return_Empty_List_Initially_AcrossAllProviders(DatabaseProvider provider)
    {
        using var testInstance = new PeopleControllerMultiProviderTests(provider, _output);
        _output.WriteLine($"Testing GET people with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await testInstance.AuthenticatedGetAsync("/api/people");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var people = await testInstance.ReadJsonAsync<List<PersonDto>>(response);
            people.Should().NotBeNull();
            people.Should().BeEmpty($"Initial people list should be empty for {testInstance.ProviderName}");
        });
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task POST_People_Should_Create_Person_AcrossAllProviders(DatabaseProvider provider)
    {
        using var testInstance = new PeopleControllerMultiProviderTests(provider, _output);
        _output.WriteLine($"Testing POST person creation with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var personName = testInstance.CreateProviderSpecificTestData("John Doe");
            var createRequest = TestDataBuilders.CreatePersonRequest(personName, "+1234567890");

            // Act
            var response = await testInstance.AuthenticatedPostJsonAsync("/api/people", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"Person creation should succeed for {testInstance.ProviderName}");

            var createdPerson = await testInstance.ReadJsonAsync<PersonDto>(response);
            createdPerson.Should().NotBeNull();
            createdPerson!.Id.Should().NotBeEmpty();
            createdPerson.FullName.Should().Be(personName);
            createdPerson.Phone.Should().Be("+1234567890");
        });
    }

    [Theory]
    [MemberData(nameof(GetAllProviders))]
    public async Task POST_People_With_Roles_Should_Handle_Relationships_AcrossAllProviders(DatabaseProvider provider)
    {
        using var testInstance = new PeopleControllerMultiProviderTests(provider, _output);
        _output.WriteLine($"Testing person-role relationships with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create a role first
            var roleName = testInstance.CreateProviderSpecificTestData("TestRole");
            var createRoleRequest = TestDataBuilders.CreateRoleRequest(roleName, "Test role");
            var roleResponse = await testInstance.AuthenticatedPostJsonAsync("/api/roles", createRoleRequest);
            var createdRole = await testInstance.ReadJsonAsync<RoleDto>(roleResponse);

            // Create person with role
            var personName = testInstance.CreateProviderSpecificTestData("Jane Doe");
            var createPersonRequest = TestDataBuilders.CreatePersonRequest(
                personName,
                "+1234567890",
                new[] { createdRole!.Id });

            // Act
            var personResponse = await testInstance.AuthenticatedPostJsonAsync("/api/people", createPersonRequest);

            // Assert
            personResponse.StatusCode.Should().Be(HttpStatusCode.Created,
                $"Person with role creation should succeed for {testInstance.ProviderName}");

            var createdPerson = await testInstance.ReadJsonAsync<PersonDto>(personResponse);
            createdPerson.Should().NotBeNull();
            createdPerson!.FullName.Should().Be(personName);
            createdPerson.Roles.Should().HaveCount(1);
            createdPerson.Roles.First().Id.Should().Be(createdRole.Id);
            createdPerson.Roles.First().Name.Should().Be(roleName);
        });
    }

    [Theory]
    [MemberData(nameof(GetForeignKeyProviders))]
    public async Task DELETE_Role_With_People_Should_Handle_Constraints_ForConstraintProviders(DatabaseProvider provider)
    {
        // This test only runs on providers that enforce foreign key constraints
        using var testInstance = new PeopleControllerMultiProviderTests(provider, _output);
        _output.WriteLine($"Testing FK constraint handling with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange - Create role and person with that role
            var roleName = testInstance.CreateProviderSpecificTestData("ConstraintTestRole");
            var createRoleRequest = TestDataBuilders.CreateRoleRequest(roleName, "Role for constraint test");
            var roleResponse = await testInstance.AuthenticatedPostJsonAsync("/api/roles", createRoleRequest);
            var createdRole = await testInstance.ReadJsonAsync<RoleDto>(roleResponse);

            var personName = testInstance.CreateProviderSpecificTestData("Person With Role");
            var createPersonRequest = TestDataBuilders.CreatePersonRequest(
                personName,
                "+1234567890",
                new[] { createdRole!.Id });
            await testInstance.AuthenticatedPostJsonAsync("/api/people", createPersonRequest);

            // Act - Try to delete the role that's referenced by a person
            var deleteResponse = await testInstance.AuthenticatedDeleteAsync($"/api/roles/{createdRole.Id}");

            // Assert - This should fail due to FK constraint (exact behavior depends on implementation)
            // The important thing is that the provider enforces referential integrity
            deleteResponse.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict,
                HttpStatusCode.UnprocessableEntity)
                .And.Subject.Should().NotBe(HttpStatusCode.NoContent,
                $"Role deletion should be prevented by FK constraints for {testInstance.ProviderName}");
        });
    }

    [Theory]
    [MemberData(nameof(GetTransactionProviders))]
    public async Task Create_Multiple_People_Should_Support_Transactions_ForTransactionProviders(DatabaseProvider provider)
    {
        // This test only runs on providers that support transactions
        using var testInstance = new PeopleControllerMultiProviderTests(provider, _output);
        _output.WriteLine($"Testing transaction support with {testInstance.ProviderName} provider");

        await testInstance.RunWithCleanDatabaseAsync(async () =>
        {
            // This test would require transaction support in the API layer
            // For now, just verify we can create multiple people successfully
            var person1Name = testInstance.CreateProviderSpecificTestData("Person One");
            var person2Name = testInstance.CreateProviderSpecificTestData("Person Two");

            var createRequest1 = TestDataBuilders.CreatePersonRequest(person1Name);
            var createRequest2 = TestDataBuilders.CreatePersonRequest(person2Name);

            // Act
            var response1 = await testInstance.AuthenticatedPostJsonAsync("/api/people", createRequest1);
            var response2 = await testInstance.AuthenticatedPostJsonAsync("/api/people", createRequest2);

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.Created);
            response2.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify both people exist
            var getAllResponse = await testInstance.AuthenticatedGetAsync("/api/people");
            var allPeople = await testInstance.ReadJsonAsync<List<PersonDto>>(getAllResponse);
            allPeople.Should().HaveCount(2);
            allPeople.Should().Contain(p => p.FullName == person1Name);
            allPeople.Should().Contain(p => p.FullName == person2Name);
        });
    }

    public static IEnumerable<object[]> GetAllProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetAllProvidersAsTestData();

    public static IEnumerable<object[]> GetForeignKeyProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetProvidersWithFeaturesAsTestData(requiresForeignKeys: true);

    public static IEnumerable<object[]> GetTransactionProviders()
        => MultiProviderTestWebApplicationFactoryProvider.GetProvidersWithFeaturesAsTestData(requiresTransactions: true);
}

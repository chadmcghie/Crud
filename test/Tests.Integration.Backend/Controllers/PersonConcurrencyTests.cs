using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using FluentAssertions;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Controllers;

public class PersonConcurrencyTests : IntegrationTestBase
{
    public PersonConcurrencyTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }
    
    [Fact]
    public async Task Test1_Verify_SQLite_RowVersion_Generation()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            
            // Act 1: Create person (no roles)
            var createRequest = TestDataBuilders.CreatePersonRequest("Test Person", "123-456-7890", roleIds: null);
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            
            // Assert 1: Person created successfully
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);
            
            // Act 2: Get person to check RowVersion
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson!.Id}");
            var retrievedPerson = await ReadJsonAsync<PersonResponse>(getResponse);
            
            // Assert 2: RowVersion should be populated
            Console.WriteLine($"Created Person RowVersion: {(retrievedPerson!.RowVersion != null ? Convert.ToBase64String(retrievedPerson.RowVersion) : "NULL")}");
            
            // Test Result: Check if SQLite generates RowVersion
            if (retrievedPerson.RowVersion == null || retrievedPerson.RowVersion.Length == 0)
            {
                Console.WriteLine("❌ TEST 1 RESULT: SQLite does NOT auto-generate RowVersion values");
            }
            else
            {
                Console.WriteLine($"✅ TEST 1 RESULT: SQLite generates RowVersion: {Convert.ToBase64String(retrievedPerson.RowVersion)}");
            }
        });
    }

    [Fact]
    public async Task Test1b_Update_Person_Properties_Only_No_Roles()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            
            // Create person
            var createRequest = TestDataBuilders.CreatePersonRequest("Original Name", "111-111-1111", roleIds: null);
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);
            
            // Get current state
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson!.Id}");
            var currentPerson = await ReadJsonAsync<PersonResponse>(getResponse);
            
            Console.WriteLine($"Before Update RowVersion: {(currentPerson!.RowVersion != null ? Convert.ToBase64String(currentPerson.RowVersion) : "NULL")}");
            
            // Act: Update person properties (no roles)
            var updateRequest = TestDataBuilders.UpdatePersonRequest("Updated Name", "222-222-2222", roleIds: null, currentPerson.RowVersion);
            var updateResponse = await adminClient.PutAsJsonAsync($"/api/people/{createdPerson.Id}", updateRequest);
            
            // Assert
            Console.WriteLine($"Update Response Status: {updateResponse.StatusCode}");
            
            if (updateResponse.StatusCode == HttpStatusCode.Conflict)
            {
                Console.WriteLine("❌ TEST 1b RESULT: Property-only update fails with 409 - confirms concurrency issue");
            }
            else if (updateResponse.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine("✅ TEST 1b RESULT: Property-only update succeeds - issue is specific to roles");
                
                // Check if RowVersion changed
                var updatedGetResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
                var updatedPerson = await ReadJsonAsync<PersonResponse>(updatedGetResponse);
                
                var originalBase64 = currentPerson.RowVersion != null ? Convert.ToBase64String(currentPerson.RowVersion) : "NULL";
                var updatedBase64 = updatedPerson!.RowVersion != null ? Convert.ToBase64String(updatedPerson.RowVersion) : "NULL";
                
                Console.WriteLine($"After Update RowVersion: {updatedBase64}");
                Console.WriteLine($"RowVersion Changed: {originalBase64 != updatedBase64}");
            }
        });
    }

    [Fact]
    public async Task Test3_Isolate_Role_Update_Issue()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();
            
            // Create roles
            var role1Request = TestDataBuilders.CreateRoleRequest("TestRole1", "Test Role 1");
            var role1Response = await adminClient.PostAsJsonAsync("/api/roles", role1Request);
            var role1 = await ReadJsonAsync<RoleResponse>(role1Response);
            
            var role2Request = TestDataBuilders.CreateRoleRequest("TestRole2", "Test Role 2");
            var role2Response = await adminClient.PostAsJsonAsync("/api/roles", role2Request);
            var role2 = await ReadJsonAsync<RoleResponse>(role2Response);
            
            // Create person with one role
            var createRequest = TestDataBuilders.CreatePersonRequest("Role Test Person", "555-555-5555", new[] { role1!.Id });
            var createResponse = await adminClient.PostAsJsonAsync("/api/people", createRequest);
            var createdPerson = await ReadJsonAsync<PersonResponse>(createResponse);
            
            Console.WriteLine($"Created person with {createdPerson!.Roles.Count()} roles");
            
            // Get current state
            var getResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
            var currentPerson = await ReadJsonAsync<PersonResponse>(getResponse);
            
            Console.WriteLine($"Retrieved person with {currentPerson!.Roles.Count()} roles, RowVersion: {(currentPerson.RowVersion != null ? Convert.ToBase64String(currentPerson.RowVersion) : "NULL")}");
            
            // Act: Update person roles (no property changes, only roles)
            var updateRequest = TestDataBuilders.UpdatePersonRequest(currentPerson.FullName, currentPerson.Phone, new[] { role2!.Id }, currentPerson.RowVersion);
            var updateResponse = await adminClient.PutAsJsonAsync($"/api/people/{createdPerson.Id}", updateRequest);
            
            // Assert
            Console.WriteLine($"Role Update Response Status: {updateResponse.StatusCode}");
            
            if (updateResponse.StatusCode == HttpStatusCode.Conflict)
            {
                Console.WriteLine("❌ TEST 3 RESULT: Role update fails with 409 - there's a deeper issue with role handling");
            }
            else if (updateResponse.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine("✅ TEST 3 RESULT: Role update succeeds without concurrency tokens");
                
                // Verify the role was actually updated
                var updatedGetResponse = await adminClient.GetAsync($"/api/people/{createdPerson.Id}");
                var updatedPerson = await ReadJsonAsync<PersonResponse>(updatedGetResponse);
                
                Console.WriteLine($"Updated person has {updatedPerson!.Roles.Count()} roles: {string.Join(", ", updatedPerson.Roles.Select(r => r.Name))}");
            }
        });
    }
}
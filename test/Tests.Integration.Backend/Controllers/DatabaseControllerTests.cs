using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Controllers;
using App.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Controllers;

public class DatabaseControllerTests : IntegrationTestBase
{
    public DatabaseControllerTests(TestWebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_Database_Status_Should_Return_Database_Statistics()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await Client.GetAsync("/api/database/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            // Basic validation that the response contains expected properties
            Assert.Contains("environment", content);
            Assert.Contains("canConnect", content);
            Assert.Contains("peopleCount", content);
            Assert.Contains("rolesCount", content);
        });
    }

    [Fact]
    public async Task POST_Database_Seed_Should_Seed_Database_Successfully()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var seedRequest = new DatabaseSeedRequest { WorkerIndex = 1 };

            // Act
            var response = await Client.PostAsJsonAsync("/api/database/seed", seedRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Database seeded successfully", content);
            Assert.Contains("\"workerIndex\":1", content);
        });
    }

    [Fact]
    public async Task GET_Database_ValidatePreTest_Should_Return_Validation_Result()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await Client.GetAsync("/api/database/validate-pre-test?workerIndex=1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Deserialize<DatabaseValidationResult>(content, options);

            Assert.NotNull(result);
            Assert.Equal(1, result!.WorkerIndex);
            Assert.Equal("PreTest", result.ValidationType);
            Assert.NotNull(result.Stats);
        });
    }

    [Fact]
    public async Task POST_Database_Reset_Should_Reset_Database_With_Valid_Token()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var resetRequest = new DatabaseResetRequest { WorkerIndex = 1, SeedData = false };
            Client.DefaultRequestHeaders.Add("X-Test-Reset-Token", "test-only-token");

            // Act
            var response = await Client.PostAsJsonAsync("/api/database/reset", resetRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Database reset successfully", content);
            Assert.Contains("\"workerIndex\":1", content);
            Assert.Contains("duration", content);
        });
    }

    [Fact]
    public async Task POST_Database_Reset_Should_Return_Unauthorized_Without_Valid_Token()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var resetRequest = new DatabaseResetRequest { WorkerIndex = 1, SeedData = false };
            // Don't set the token header

            // Act
            var response = await Client.PostAsJsonAsync("/api/database/reset", resetRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        });
    }

    [Fact]
    public async Task GET_Database_VerifyIntegrity_Should_Return_Integrity_Check_Result()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await Client.GetAsync("/api/database/verify-integrity?workerIndex=1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("isValid", content);
            Assert.Contains("\"workerIndex\":1", content);
        });
    }
}

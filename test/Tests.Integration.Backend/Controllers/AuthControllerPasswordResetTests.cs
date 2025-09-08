using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api;
using App.Features.Authentication;
using Domain.Entities.Authentication;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Controllers;

public class AuthControllerPasswordResetTests : IntegrationTestBase
{
    public AuthControllerPasswordResetTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    private async Task<User> SeedTestUserAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Create(
            new Domain.ValueObjects.Email("test@example.com"),
            new Domain.ValueObjects.PasswordHash("$2a$11$K5Y7Tc5kZPCK7CJnBpRCY.Rk7UqFp7YZ3pEZWx9xGKOq6Y/2YxLha"),
            "Test",
            "User"
        );

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task POST_ForgotPassword_WithValidEmail_Should_Return_Success()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            await SeedTestUserAsync();
            var request = new ForgotPasswordCommand
            {
                Email = "test@example.com"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task POST_ForgotPassword_WithInvalidEmail_Should_Return_BadRequest()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var request = new ForgotPasswordCommand
            {
                Email = "invalid-email"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("error").GetString().Should().Contain("format");
        });
    }

    [Fact]
    public async Task POST_ForgotPassword_WithNonExistentEmail_Should_Return_Success()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var request = new ForgotPasswordCommand
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            // Should return success to prevent email enumeration
            result.GetProperty("message").GetString().Should().Contain("If the email exists");
        });
    }

    [Fact]
    public async Task POST_ValidateResetToken_WithValidToken_Should_Return_Valid_Status()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            await SeedTestUserAsync();

            // First create a token through forgot password
            var forgotPasswordRequest = new ForgotPasswordCommand
            {
                Email = "test@example.com"
            };
            await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", forgotPasswordRequest);

            // Get the token from database (in real scenario, this would come from email)
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = await dbContext.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            token.Should().NotBeNull();

            var validateRequest = new ValidateResetTokenQuery
            {
                Token = token!.Token
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/validate-reset-token", validateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("isValid").GetBoolean().Should().BeTrue();
            result.GetProperty("isExpired").GetBoolean().Should().BeFalse();
            result.GetProperty("isUsed").GetBoolean().Should().BeFalse();
        });
    }

    [Fact]
    public async Task POST_ValidateResetToken_WithInvalidToken_Should_Return_Invalid_Status()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var request = new ValidateResetTokenQuery
            {
                Token = "invalid-token-123"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/validate-reset-token", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("isValid").GetBoolean().Should().BeFalse();
        });
    }

    [Fact]
    public async Task POST_ResetPassword_WithValidToken_Should_Reset_Password_Successfully()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            await SeedTestUserAsync();

            // First create a token through forgot password
            var forgotPasswordRequest = new ForgotPasswordCommand
            {
                Email = "test@example.com"
            };
            await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", forgotPasswordRequest);

            // Get the token from database
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = await dbContext.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            token.Should().NotBeNull();

            var resetRequest = new ResetPasswordCommand
            {
                Token = token!.Token,
                NewPassword = "NewP@ssw0rd123!"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/reset-password", resetRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("message").GetString().Should().Contain("successfully");

            // Verify token is marked as used
            await dbContext.Entry(token).ReloadAsync();
            token.IsUsed.Should().BeTrue();
        });
    }

    [Fact]
    public async Task POST_ResetPassword_WithInvalidToken_Should_Return_Error()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var request = new ResetPasswordCommand
            {
                Token = "invalid-token-123",
                NewPassword = "NewP@ssw0rd123!"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/reset-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("error").GetString().Should().Contain("Invalid");
        });
    }

    [Fact]
    public async Task POST_ResetPassword_WithWeakPassword_Should_Return_Error()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            await SeedTestUserAsync();

            // First create a token through forgot password
            var forgotPasswordRequest = new ForgotPasswordCommand
            {
                Email = "test@example.com"
            };
            await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", forgotPasswordRequest);

            // Get the token from database
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = await dbContext.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            token.Should().NotBeNull();

            var resetRequest = new ResetPasswordCommand
            {
                Token = token!.Token,
                NewPassword = "weak"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/reset-password", resetRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            result.GetProperty("error").GetString().Should().Contain("Password must");
        });
    }

    [Fact]
    public async Task Complete_Password_Reset_Workflow_Should_Work()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            await SeedTestUserAsync();
            var email = "test@example.com";

            // Step 1: Request password reset
            var forgotPasswordRequest = new ForgotPasswordCommand { Email = email };
            var forgotResponse = await PostJsonWithErrorLoggingAsync("/api/auth/forgot-password", forgotPasswordRequest);
            forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: Get token from database (simulating email)
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = await dbContext.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            token.Should().NotBeNull();

            // Step 3: Validate token
            var validateRequest = new ValidateResetTokenQuery { Token = token!.Token };
            var validateResponse = await PostJsonWithErrorLoggingAsync("/api/auth/validate-reset-token", validateRequest);
            validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var validateContent = await validateResponse.Content.ReadAsStringAsync();
            var validateResult = JsonSerializer.Deserialize<JsonElement>(validateContent);
            validateResult.GetProperty("isValid").GetBoolean().Should().BeTrue();

            // Step 4: Reset password
            var resetRequest = new ResetPasswordCommand
            {
                Token = token.Token,
                NewPassword = "NewSecureP@ssw0rd123!"
            };
            var resetResponse = await PostJsonWithErrorLoggingAsync("/api/auth/reset-password", resetRequest);
            resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 5: Verify token is now invalid
            var revalidateResponse = await PostJsonWithErrorLoggingAsync("/api/auth/validate-reset-token", validateRequest);
            revalidateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var revalidateContent = await revalidateResponse.Content.ReadAsStringAsync();
            var revalidateResult = JsonSerializer.Deserialize<JsonElement>(revalidateContent);
            revalidateResult.GetProperty("isValid").GetBoolean().Should().BeFalse();
            revalidateResult.GetProperty("isUsed").GetBoolean().Should().BeTrue();

            // Step 6: Verify can't use the same token again
            var secondResetResponse = await PostJsonWithErrorLoggingAsync("/api/auth/reset-password", resetRequest);
            secondResetResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errorContent = await secondResetResponse.Content.ReadAsStringAsync();
            var errorResult = JsonSerializer.Deserialize<JsonElement>(errorContent);
            errorResult.GetProperty("error").GetString().Should().Contain("already been used");
        });
    }
}

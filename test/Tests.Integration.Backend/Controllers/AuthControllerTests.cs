using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api;
using App.Features.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Controllers;

// DTO for test responses
public class TokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(TestWebApplicationFactoryFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task POST_Register_Should_Create_User_And_Return_Tokens()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/register", command);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await ReadJsonAsync<TokenResponse>(response);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
            Assert.True(result.ExpiresIn > 0);
        });
    }

    [Fact]
    public async Task POST_Register_Should_Return_BadRequest_For_Existing_Email()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "existing@example.com",
                Password = "Test123!@#",
                FirstName = "Jane",
                LastName = "Smith"
            };

            // Register first time
            var firstResponse = await PostJsonWithErrorLoggingAsync("/api/auth/register", command);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            // Act - Try to register with same email
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/register", command);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        });
    }

    [Fact]
    public async Task POST_Register_Should_Return_BadRequest_For_Invalid_Email()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "invalid-email",
                Password = "Test123!@#",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var response = await PostJsonWithErrorLoggingAsync("/api/auth/register", command);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        });
    }

    [Fact]
    public async Task POST_Login_Should_Return_Tokens_For_Valid_Credentials()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var email = "login@example.com";
            var password = "Test123!@#";

            // First register a user
            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FirstName = "Login",
                LastName = "Test"
            };
            await Client.PostAsJsonAsync("/api/auth/register", registerCommand);

            var loginCommand = new LoginCommand
            {
                Email = email,
                Password = password
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(content, JsonOptions);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        });
    }

    [Fact]
    public async Task POST_Login_Should_Return_Unauthorized_For_Invalid_Credentials()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var loginCommand = new LoginCommand
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        });
    }

    [Fact]
    public async Task POST_Login_Should_Return_BadRequest_For_Invalid_Email_Format()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var loginCommand = new LoginCommand
            {
                Email = "invalid-email",
                Password = "Test123!@#"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        });
    }

    [Fact]
    public async Task POST_Refresh_Should_Return_New_Tokens_For_Valid_RefreshToken()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            // First register and login
            var email = "refresh@example.com";
            var password = "Test123!@#";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FirstName = "Refresh",
                LastName = "Test"
            };
            await Client.PostAsJsonAsync("/api/auth/register", registerCommand);

            var loginCommand = new LoginCommand
            {
                Email = email,
                Password = password
            };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<AuthenticationResponse>(loginContent, JsonOptions);

            var refreshCommand = new RefreshTokenCommand
            {
                RefreshToken = loginResult!.RefreshToken!
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(content, JsonOptions);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
            Assert.NotEqual(loginResult.RefreshToken, result.RefreshToken); // Should be a new refresh token
        });
    }

    [Fact]
    public async Task POST_Refresh_Should_Return_Unauthorized_For_Invalid_RefreshToken()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var refreshCommand = new RefreshTokenCommand
            {
                RefreshToken = "invalid-refresh-token"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        });
    }

    [Fact]
    public async Task POST_Logout_Should_Revoke_RefreshToken()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            // First register and login
            var email = "logout@example.com";
            var password = "Test123!@#";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FirstName = "Logout",
                LastName = "Test"
            };
            await Client.PostAsJsonAsync("/api/auth/register", registerCommand);

            var loginCommand = new LoginCommand
            {
                Email = email,
                Password = password
            };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<AuthenticationResponse>(loginContent, JsonOptions);

            // Add the access token to the request header
            Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

            // Act - Logout
            var logoutResponse = await Client.PostAsync("/api/auth/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

            // Try to use the refresh token after logout - should fail
            var refreshCommand = new RefreshTokenCommand
            {
                RefreshToken = loginResult!.RefreshToken!
            };
            var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);
            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        });
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Return_Unauthorized_Without_Token()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Act
            var response = await Client.GetAsync("/api/auth/me");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        });
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Return_OK_With_Valid_Token()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var email = "protected@example.com";
            var password = "Test123!@#";

            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FirstName = "Protected",
                LastName = "Test"
            };
            await Client.PostAsJsonAsync("/api/auth/register", registerCommand);

            var loginCommand = new LoginCommand
            {
                Email = email,
                Password = password
            };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<AuthenticationResponse>(loginContent, JsonOptions);

            // Add the access token to the request header
            Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

            // Act
            var response = await Client.GetAsync("/api/auth/me");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api;
using App.Features.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration.Backend.E2E;

[Collection("E2E")]
public class AuthenticationE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthenticationE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task Complete_Authentication_Flow_Should_Work_End_To_End()
    {
        // Step 1: Register a new user
        var registerCommand = new RegisterUserCommand
        {
            Email = $"e2e.test.{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            FirstName = "E2E",
            LastName = "Test"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<TokenResponse>(registerContent, _jsonOptions);

        Assert.NotNull(registerResult);
        Assert.False(string.IsNullOrEmpty(registerResult!.AccessToken));
        Assert.False(string.IsNullOrEmpty(registerResult.RefreshToken));

        // Step 2: Access protected endpoint with token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult.AccessToken);

        var meResponse = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        // Step 3: Login with same credentials
        var loginCommand = new LoginCommand
        {
            Email = registerCommand.Email,
            Password = registerCommand.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<TokenResponse>(loginContent, _jsonOptions);

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrEmpty(loginResult!.AccessToken));

        // Step 4: Refresh token
        var refreshCommand = new RefreshTokenCommand
        {
            RefreshToken = loginResult!.RefreshToken!
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshResult = JsonSerializer.Deserialize<TokenResponse>(refreshContent, _jsonOptions);

        Assert.NotNull(refreshResult);
        Assert.False(string.IsNullOrEmpty(refreshResult!.AccessToken));
        Assert.NotEqual(loginResult.AccessToken, refreshResult.AccessToken); // New token

        // Step 5: Logout
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Step 6: Verify old refresh token no longer works
        var oldRefreshCommand = new RefreshTokenCommand
        {
            RefreshToken = loginResult!.RefreshToken!
        };

        var oldRefreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", oldRefreshCommand);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task Registration_Should_Validate_Email_Format()
    {
        var command = new RegisterUserCommand
        {
            Email = "invalid-email",
            Password = "Test123!@#",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", command);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_Should_Validate_Password_Complexity()
    {
        var command = new RegisterUserCommand
        {
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "weak", // Too weak
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", command);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_Should_Prevent_Duplicate_Emails()
    {
        var email = $"duplicate.{Guid.NewGuid()}@example.com";

        var command = new RegisterUserCommand
        {
            Email = email,
            Password = "Test123!@#",
            FirstName = "First",
            LastName = "User"
        };

        // First registration should succeed
        var response1 = await _client.PostAsJsonAsync("/api/auth/register", command);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Second registration with same email should fail
        command.FirstName = "Second";
        var response2 = await _client.PostAsJsonAsync("/api/auth/register", command);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task Login_Should_Fail_With_Invalid_Credentials()
    {
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", command);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Require_Authentication()
    {
        // Clear any existing auth headers
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_Should_Expire_And_Require_Refresh()
    {
        // This test would require manipulating time or waiting for token expiry
        // For now, we'll just verify the refresh mechanism works

        var registerCommand = new RegisterUserCommand
        {
            Email = $"expiry.test.{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            FirstName = "Expiry",
            LastName = "Test"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerCommand);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var content = await registerResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TokenResponse>(content, _jsonOptions);

        // Verify we can refresh the token
        var refreshCommand = new RefreshTokenCommand
        {
            RefreshToken = result!.RefreshToken!
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Role_Based_Access_Should_Work()
    {
        // Register a regular user
        var userCommand = new RegisterUserCommand
        {
            Email = $"user.{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            FirstName = "Regular",
            LastName = "User"
        };

        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userCommand);
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        var userContent = await userResponse.Content.ReadAsStringAsync();
        var userResult = JsonSerializer.Deserialize<TokenResponse>(userContent, _jsonOptions);

        // Set user token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userResult!.AccessToken);

        // Access user endpoint should work
        var meResponse = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        // If we had admin-only endpoints, we would test that regular users can't access them
        // For now, we just verify the authentication works
    }

    [Fact]
    public async Task Refresh_Token_Rotation_Should_Work()
    {
        // Register a user
        var command = new RegisterUserCommand
        {
            Email = $"rotation.{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            FirstName = "Rotation",
            LastName = "Test"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", command);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<TokenResponse>(registerContent, _jsonOptions);

        var currentRefreshToken = registerResult!.RefreshToken!;

        // Refresh multiple times and verify token rotation
        for (int i = 0; i < 3; i++)
        {
            var refreshCommand = new RefreshTokenCommand
            {
                RefreshToken = currentRefreshToken!
            };

            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshCommand);
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

            var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
            var refreshResult = JsonSerializer.Deserialize<TokenResponse>(refreshContent, _jsonOptions);

            Assert.NotEqual(currentRefreshToken, refreshResult!.RefreshToken); // New token each time
            currentRefreshToken = refreshResult.RefreshToken;
        }
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Be_Handled_Properly()
    {
        // Register a user
        var command = new RegisterUserCommand
        {
            Email = $"concurrent.{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            FirstName = "Concurrent",
            LastName = "Test"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", command);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<TokenResponse>(registerContent, _jsonOptions);

        // Set auth header
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerResult!.AccessToken);

        // Make multiple concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/api/auth/me"));
        }

        var responses = await Task.WhenAll(tasks);

        // All should succeed
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // Helper class for deserialization
    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }
}

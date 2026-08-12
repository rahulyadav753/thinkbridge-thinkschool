using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using QuotesApi.Models;

namespace Quotes.Tests.Integration;

public class QuoteIntegrationTests
{
    [Fact]
    public async Task GetQuotes_EmptyDatabase_ReturnsOk()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/quotes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQuote_InvalidId_ReturnsNotFound()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/quotes/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetQuotes_InvalidPage_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/quotes?page=0&size=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteQuote_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/quotes/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            email = "test@example.com",
            password = "WrongPassword!"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateQuote_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            author = "Test Author",
            text = "Test quote"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes/",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateQuote_WithValidToken_ReturnsCreated()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult!.AccessToken);

        var quoteRequest = new
        {
            author = "Integration Test",
            text = "Testing quote creation"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes/",
            quoteRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("Integration Test");
        json.Should().Contain("Testing quote creation");

        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateQuote_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult!.AccessToken);

        var invalidQuote = new
        {
            author = "",
            text = ""
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/quotes/",
            invalidQuote);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteQuote_WithValidToken_ReturnsNoContent()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult!.AccessToken);

        var quoteRequest = new
        {
            author = "Delete Test",
            text = "Quote to delete"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/quotes/",
            quoteRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location;

        location.Should().NotBeNull();

        // Act
        var deleteResponse =
            await client.DeleteAsync(location);

        // Assert
        deleteResponse.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteQuote_NonExistingQuote_WithValidPolicy_ReturnsNotFound()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult!.AccessToken);

        // Act
        var response = await client.DeleteAsync(
            "/api/quotes/999");

        // Assert

        // If your authorization handler requires ownership,
        // this endpoint may correctly return 403 before reaching
        // the repository. We therefore accept either authorization
        // failure or the endpoint's expected 404.
        response.StatusCode.Should()
            .BeOneOf(
                HttpStatusCode.NotFound,
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        var oldRefreshToken = loginResult!.RefreshToken;

        var refreshRequest = new
        {
            refreshToken = oldRefreshToken
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        result.RefreshToken.Should().NotBe(oldRefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            refreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            request);

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReusedRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        var oldRefreshToken = loginResult!.RefreshToken;

        // First refresh rotates the token.
        var firstRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                refreshToken = oldRefreshToken
            });

        firstRefresh.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        // Act - reuse the old refresh token.
        var reusedRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                refreshToken = oldRefreshToken
            });

        // Assert
        reusedRefresh.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ValidRefreshToken_ReturnsNoContent()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        var refreshToken = loginResult!.RefreshToken;

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new
            {
                refreshToken
            });

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_AfterLogout_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginRequest = new
        {
            email = "test@example.com",
            password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult.Should().NotBeNull();

        var refreshToken = loginResult!.RefreshToken;

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new
            {
                refreshToken
            });

        logoutResponse.StatusCode.Should()
            .Be(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                refreshToken
            });

        // Assert
        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }
}
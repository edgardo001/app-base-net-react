using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Services;

public class GitHubAuthServiceTests
{
    private readonly Mock<IOptions<GitHubOptions>> _optionsMock = new();
    private readonly Mock<ILogger<GitHubAuthService>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _handlerMock = new();
    private readonly HttpClient _httpClient;
    private readonly GitHubOptions _options;

    public GitHubAuthServiceTests()
    {
        _options = new GitHubOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUri = "http://localhost:5011/api/auth/github/callback"
        };
        _optionsMock.Setup(x => x.Value).Returns(_options);
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);
    }

    [Fact]
    public void GetAuthorizationUrl_ReturnsCorrectUrl()
    {
        var service = new GitHubAuthService(_optionsMock.Object, _loggerMock.Object, _httpClient);
        var state = "test-state";

        var url = service.GetAuthorizationUrl(state);

        url.Should().StartWith("https://github.com/login/oauth/authorize?");
        url.Should().Contain($"client_id={_options.ClientId}");
        url.Should().Contain($"redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}");
        url.Should().Contain($"scope={Uri.EscapeDataString("read:user user:email")}");
        url.Should().Contain($"state={state}");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidState_ThrowsInvalidOperationException()
    {
        var service = new GitHubAuthService(_optionsMock.Object, _loggerMock.Object, _httpClient);

        var act = () => service.ExchangeCodeAsync("code", "nonexistent-state", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid or expired state parameter");
    }

    [Fact]
    public async Task ExchangeCodeAsync_SuccessfulExchange_ReturnsGitHubUserInfo()
    {
        var state = "valid-state";
        var accessToken = "gho_test_token";

        var tokenResponse = new
        {
            access_token = accessToken,
            token_type = "bearer",
            scope = "read:user,user:email"
        };

        var userResponse = new
        {
            id = 12345L,
            login = "octocat",
            name = "Octocat Name",
            email = "octocat@github.com"
        };

        var tokenJson = JsonSerializer.Serialize(tokenResponse);
        var userJson = JsonSerializer.Serialize(userResponse);

        _handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenJson, System.Text.Encoding.UTF8, "application/json")
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userJson, System.Text.Encoding.UTF8, "application/json")
            });

        var service = new GitHubAuthService(_optionsMock.Object, _loggerMock.Object, _httpClient);
        service.GetAuthorizationUrl(state);

        var result = await service.ExchangeCodeAsync("valid-code", state, CancellationToken.None);

        result.ProviderId.Should().Be("12345");
        result.Email.Should().Be("octocat@github.com");
        result.FirstName.Should().Be("Octocat");
        result.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithoutName_UsesLoginAsFallback()
    {
        var state = "valid-state-2";
        var accessToken = "gho_test_token_2";

        var tokenResponse = new
        {
            access_token = accessToken,
            token_type = "bearer",
            scope = "read:user,user:email"
        };

        var userResponse = new
        {
            id = 67890L,
            login = "octocat",
            name = (string?)null,
            email = (string?)null
        };

        var emailsResponse = new object[]
        {
            new { email = "octocat-private@github.com", primary = true, verified = true }
        };

        var tokenJson = JsonSerializer.Serialize(tokenResponse);
        var userJson = JsonSerializer.Serialize(userResponse);
        var emailsJson = JsonSerializer.Serialize(emailsResponse);

        _handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenJson, System.Text.Encoding.UTF8, "application/json")
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userJson, System.Text.Encoding.UTF8, "application/json")
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(emailsJson, System.Text.Encoding.UTF8, "application/json")
            });

        var service = new GitHubAuthService(_optionsMock.Object, _loggerMock.Object, _httpClient);
        service.GetAuthorizationUrl(state);

        var result = await service.ExchangeCodeAsync("code-2", state, CancellationToken.None);

        result.ProviderId.Should().Be("67890");
        result.Email.Should().Be("octocat-private@github.com");
        result.FirstName.Should().Be("octocat");
        result.LastName.Should().Be("");
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithoutNameAndEmail_UsesLoginAndFallbackEmail()
    {
        var state = "valid-state-3";
        var accessToken = "gho_test_token_3";

        var tokenResponse = new
        {
            access_token = accessToken,
            token_type = "bearer",
            scope = "read:user,user:email"
        };

        var userResponse = new
        {
            id = 11111L,
            login = "anon-user",
            name = (string?)null,
            email = (string?)null
        };

        var emailsResponse = Array.Empty<object>();

        var tokenJson = JsonSerializer.Serialize(tokenResponse);
        var userJson = JsonSerializer.Serialize(userResponse);
        var emailsJson = JsonSerializer.Serialize(emailsResponse);

        _handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenJson, System.Text.Encoding.UTF8, "application/json")
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userJson, System.Text.Encoding.UTF8, "application/json")
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(emailsJson, System.Text.Encoding.UTF8, "application/json")
            });

        var service = new GitHubAuthService(_optionsMock.Object, _loggerMock.Object, _httpClient);
        service.GetAuthorizationUrl(state);

        var result = await service.ExchangeCodeAsync("code-3", state, CancellationToken.None);

        result.ProviderId.Should().Be("11111");
        result.Email.Should().Be("anon-user@github.local");
        result.FirstName.Should().Be("anon-user");
        result.LastName.Should().Be("");
    }
}

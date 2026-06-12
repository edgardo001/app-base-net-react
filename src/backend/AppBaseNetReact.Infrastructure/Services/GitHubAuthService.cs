using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Infrastructure.Services;

public class GitHubAuthService : IGitHubAuthService
{
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubAuthService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly ConcurrentDictionary<string, string> StateStore = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public GitHubAuthService(IOptions<GitHubOptions> options, ILogger<GitHubAuthService> logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public string GetAuthorizationUrl(string state)
    {
        StateStore[state] = state;

        var url = $"https://github.com/login/oauth/authorize?" +
                  $"client_id={_options.ClientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                  $"&scope={Uri.EscapeDataString("read:user user:email")}" +
                  $"&state={state}";

        return url;
    }

    public async Task<GitHubUserInfo> ExchangeCodeAsync(string code, string state, CancellationToken ct = default)
    {
        _logger.LogInformation("ExchangeCodeAsync called. StateStore has {Count} entries. State length: {Length}",
            StateStore.Count, state?.Length ?? 0);

        if (!StateStore.TryRemove(state, out _))
        {
            _logger.LogWarning("State validation failed. Available states: {States}",
                string.Join(", ", StateStore.Keys.Take(5)));
            throw new InvalidOperationException("Invalid or expired state parameter");
        }

        var accessToken = await ExchangeCodeForAccessTokenAsync(code, ct);
        var user = await FetchGitHubUserAsync(accessToken, ct);

        var email = user.Email;
        if (string.IsNullOrEmpty(email))
        {
            email = await FetchPrimaryEmailAsync(accessToken, ct);
        }

        if (string.IsNullOrEmpty(email))
        {
            email = $"{user.Login}@github.local";
        }

        var firstName = user.Name ?? user.Login;
        var lastName = "";

        if (user.Name != null)
        {
            var parts = user.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            firstName = parts[0];
            lastName = parts.Length > 1 ? parts[1] : "";
        }

        return new GitHubUserInfo(
            user.Id.ToString(),
            email,
            firstName,
            lastName);
    }

    private async Task<string> ExchangeCodeForAccessTokenAsync(string code, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _options.ClientId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _options.RedirectUri)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = content
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("GitHub token exchange failed: {StatusCode} {ErrorBody}",
                (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>(JsonOptions, ct);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain access token from GitHub");
        }

        return tokenResponse.AccessToken;
    }

    private async Task<GitHubApiUser> FetchGitHubUserAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("AppBaseNetReact/1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("GitHub user API failed: {StatusCode} {ErrorBody}",
                (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var user = await response.Content.ReadFromJsonAsync<GitHubApiUser>(JsonOptions, ct);
        return user ?? throw new InvalidOperationException("Failed to deserialize GitHub user response");
    }

    private async Task<string?> FetchPrimaryEmailAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("AppBaseNetReact/1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var emails = await response.Content.ReadFromJsonAsync<List<GitHubApiEmail>>(JsonOptions, ct);
            return emails?.FirstOrDefault(e => e.Primary)?.Email;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch GitHub emails");
            return null;
        }
    }

    private class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    private class GitHubApiUser
    {
        public long Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    private class GitHubApiEmail
    {
        public string Email { get; set; } = string.Empty;
        public bool Primary { get; set; }
        public bool Verified { get; set; }
    }
}

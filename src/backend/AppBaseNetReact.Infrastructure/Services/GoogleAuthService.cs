using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleOptions _options;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly ConcurrentDictionary<string, string> StateStore = new();

    public GoogleAuthService(IOptions<GoogleOptions> options, ILogger<GoogleAuthService> logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public string GetAuthorizationUrl(string state)
    {
        StateStore[state] = state;

        var url = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                  $"response_type=code" +
                  $"&client_id={_options.ClientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                  $"&scope=openid%20email%20profile" +
                  $"&access_type=offline" +
                  $"&state={state}";

        return url;
    }

    public async Task<GoogleUserInfo> ExchangeCodeAsync(string code, string state, CancellationToken ct = default)
    {
        _logger.LogInformation("ExchangeCodeAsync called. StateStore has {Count} entries. State length: {Length}",
            StateStore.Count, state?.Length ?? 0);

        if (!StateStore.TryRemove(state, out _))
        {
            _logger.LogWarning("State validation failed. Available states: {States}",
                string.Join(", ", StateStore.Keys.Take(5)));
            throw new InvalidOperationException("Invalid or expired state parameter");
        }

        var tokenResponse = await ExchangeCodeForTokensAsync(code, ct);

        var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(
            tokenResponse.IdToken,
            new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.ClientId]
            });

        return new GoogleUserInfo(
            payload.Subject,
            payload.Email,
            payload.GivenName ?? payload.Email.Split('@')[0],
            payload.FamilyName ?? "");
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(string code, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", _options.ClientId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", _options.RedirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Google token exchange failed: {StatusCode} {ErrorBody}",
                (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct);
        return json ?? throw new InvalidOperationException("Failed to deserialize Google token response");
    }

    private class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;
    }
}

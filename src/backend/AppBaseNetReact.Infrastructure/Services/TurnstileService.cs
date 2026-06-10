using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Infrastructure.Services;

public sealed class TurnstileService : ICaptchaService
{
    private readonly TurnstileOptions _options;
    private readonly HttpClient _httpClient;

    public TurnstileService(IOptions<TurnstileOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public bool IsEnabled =>
        _options.Provider == "Cloudflare"
        && !string.IsNullOrEmpty(_options.SiteKey)
        && !string.IsNullOrEmpty(_options.SecretKey);

    public async Task<bool> VerifyTokenAsync(string token, CancellationToken ct = default)
    {
        if (!IsEnabled) return true;

        var response = await _httpClient.PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _options.SecretKey,
                ["response"] = token
            }),
            ct);

        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: ct);
        return result?.Success == true;
    }

    private sealed record TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
    }
}

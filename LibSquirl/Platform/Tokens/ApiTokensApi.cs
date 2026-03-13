using System.Text.Json.Serialization;

using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Tokens;

public sealed class ApiTokensApi(HttpClient httpClient, TursoPlatformOptions options)
    : PlatformApiBase(httpClient, options), IApiTokensApi
{
    public async Task<List<ApiToken>> ListAsync(CancellationToken cancellationToken = default)
    {
        TokensListWrapper wrapper = await GetAsync<TokensListWrapper>("/v1/auth/api-tokens", cancellationToken);
        return wrapper.Tokens;
    }

    public async Task<ApiToken> CreateAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ApiToken>($"/v1/auth/api-tokens/{tokenName}", null, cancellationToken);
    }

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        await GetAsync<object>("/v1/auth/validate", cancellationToken);
    }

    public async Task RevokeAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        await DeleteAsync<object>($"/v1/auth/api-tokens/{tokenName}", cancellationToken);
    }

    private sealed class TokensListWrapper
    {
        [JsonPropertyName("tokens")]
        public List<ApiToken> Tokens { get; set; } = [];
    }
}

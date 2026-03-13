using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Tokens;

public interface IApiTokensApi
{
    Task<List<ApiToken>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiToken> CreateAsync(string tokenName, CancellationToken cancellationToken = default);
    Task ValidateAsync(CancellationToken cancellationToken = default);
    Task RevokeAsync(string tokenName, CancellationToken cancellationToken = default);
}

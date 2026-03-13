using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Groups;

public interface IGroupsApi
{
    Task<List<Group>> ListAsync(CancellationToken cancellationToken = default);
    Task<Group> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    Task<Group> GetAsync(string groupName, CancellationToken cancellationToken = default);
    Task<Group> DeleteAsync(string groupName, CancellationToken cancellationToken = default);
    Task<Group> AddLocationAsync(string groupName, string location, CancellationToken cancellationToken = default);
    Task<Group> RemoveLocationAsync(string groupName, string location, CancellationToken cancellationToken = default);
    Task<TokenResponse> CreateTokenAsync(string groupName, string? expiration = null, string? authorization = null, CreateTokenRequest? body = null, CancellationToken cancellationToken = default);
    Task InvalidateTokensAsync(string groupName, CancellationToken cancellationToken = default);
    Task<GroupConfiguration> GetConfigurationAsync(string groupName, CancellationToken cancellationToken = default);
    Task<GroupConfiguration> UpdateConfigurationAsync(string groupName, UpdateGroupConfigurationRequest request, CancellationToken cancellationToken = default);
    Task TransferAsync(string groupName, TransferGroupRequest request, CancellationToken cancellationToken = default);
    Task UnarchiveAsync(string groupName, CancellationToken cancellationToken = default);
}

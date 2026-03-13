using System.Text.Json.Serialization;
using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Groups;

public sealed class GroupsApi(HttpClient httpClient, TursoPlatformOptions options)
    : PlatformApiBase(httpClient, options),
        IGroupsApi
{
    private string GroupsPath => $"{OrgPath}/groups";

    public async Task<List<Group>> ListAsync(CancellationToken cancellationToken = default)
    {
        GroupsListWrapper wrapper = await GetAsync<GroupsListWrapper>(
            GroupsPath,
            cancellationToken
        );
        return wrapper.Groups;
    }

    public async Task<Group> CreateAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default
    )
    {
        GroupWrapper wrapper = await PostAsync<GroupWrapper>(
            GroupsPath,
            request,
            cancellationToken
        );
        return wrapper.Group;
    }

    public async Task<Group> GetAsync(
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        GroupWrapper wrapper = await GetAsync<GroupWrapper>(
            $"{GroupsPath}/{groupName}",
            cancellationToken
        );
        return wrapper.Group;
    }

    public async Task<Group> DeleteAsync(
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        GroupWrapper wrapper = await DeleteAsync<GroupWrapper>(
            $"{GroupsPath}/{groupName}",
            cancellationToken
        );
        return wrapper.Group;
    }

    public async Task<Group> AddLocationAsync(
        string groupName,
        string location,
        CancellationToken cancellationToken = default
    )
    {
        GroupWrapper wrapper = await PostAsync<GroupWrapper>(
            $"{GroupsPath}/{groupName}/locations/{location}",
            null,
            cancellationToken
        );
        return wrapper.Group;
    }

    public async Task<Group> RemoveLocationAsync(
        string groupName,
        string location,
        CancellationToken cancellationToken = default
    )
    {
        GroupWrapper wrapper = await DeleteAsync<GroupWrapper>(
            $"{GroupsPath}/{groupName}/locations/{location}",
            cancellationToken
        );
        return wrapper.Group;
    }

    public async Task<TokenResponse> CreateTokenAsync(
        string groupName,
        string? expiration = null,
        string? authorization = null,
        CreateTokenRequest? body = null,
        CancellationToken cancellationToken = default
    )
    {
        string path = $"{GroupsPath}/{groupName}/auth/tokens";
        List<string> queryParams = [];

        if (expiration is not null)
        {
            queryParams.Add($"expiration={Uri.EscapeDataString(expiration)}");
        }

        if (authorization is not null)
        {
            queryParams.Add($"authorization={Uri.EscapeDataString(authorization)}");
        }

        if (queryParams.Count > 0)
        {
            path += "?" + string.Join("&", queryParams);
        }

        return await PostAsync<TokenResponse>(path, body, cancellationToken);
    }

    public async Task InvalidateTokensAsync(
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync<object>($"{GroupsPath}/{groupName}/auth/rotate", null, cancellationToken);
    }

    public async Task<GroupConfiguration> GetConfigurationAsync(
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        return await GetAsync<GroupConfiguration>(
            $"{GroupsPath}/{groupName}/configuration",
            cancellationToken
        );
    }

    public async Task<GroupConfiguration> UpdateConfigurationAsync(
        string groupName,
        UpdateGroupConfigurationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return await PatchAsync<GroupConfiguration>(
            $"{GroupsPath}/{groupName}/configuration",
            request,
            cancellationToken
        );
    }

    public async Task TransferAsync(
        string groupName,
        TransferGroupRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync<object>($"{GroupsPath}/{groupName}/transfer", request, cancellationToken);
    }

    public async Task UnarchiveAsync(
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        await PostAsync<object>($"{GroupsPath}/{groupName}/unarchive", null, cancellationToken);
    }

    private sealed class GroupsListWrapper
    {
        [JsonPropertyName("groups")]
        public List<Group> Groups { get; } = [];
    }

    private sealed class GroupWrapper
    {
        [JsonPropertyName("group")]
        public Group Group { get; } = null!;
    }
}

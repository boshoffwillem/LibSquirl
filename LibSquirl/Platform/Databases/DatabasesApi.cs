using System.Net.Http.Json;
using System.Text.Json.Serialization;

using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Databases;

public sealed class DatabasesApi(HttpClient httpClient, TursoPlatformOptions options)
    : PlatformApiBase(httpClient, options), IDatabasesApi
{
    private string DatabasesPath => $"{OrgPath}/databases";

    public async Task<List<Database>> ListAsync(CancellationToken cancellationToken = default)
    {
        DatabasesListWrapper wrapper = await GetAsync<DatabasesListWrapper>(DatabasesPath, cancellationToken);
        return wrapper.Databases;
    }

    public async Task<Database> CreateAsync(CreateDatabaseRequest request, CancellationToken cancellationToken = default)
    {
        DatabaseWrapper wrapper = await PostAsync<DatabaseWrapper>(DatabasesPath, request, cancellationToken);
        return wrapper.Database;
    }

    public async Task<Database> GetAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        DatabaseWrapper wrapper = await GetAsync<DatabaseWrapper>(
            $"{DatabasesPath}/{databaseName}", cancellationToken);
        return wrapper.Database;
    }

    public async Task<string> DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        DeleteWrapper wrapper = await DeleteAsync<DeleteWrapper>(
            $"{DatabasesPath}/{databaseName}", cancellationToken);
        return wrapper.Database;
    }

    public async Task<TokenResponse> CreateTokenAsync(
        string databaseName,
        string? expiration = null,
        string? authorization = null,
        CreateTokenRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        string path = $"{DatabasesPath}/{databaseName}/auth/tokens";
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

    public async Task InvalidateTokensAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        await PostAsync<object>($"{DatabasesPath}/{databaseName}/auth/rotate", null, cancellationToken);
    }

    public async Task<DatabaseStats> GetStatsAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DatabaseStats>($"{DatabasesPath}/{databaseName}/stats", cancellationToken);
    }

    public async Task<List<DatabaseInstance>> ListInstancesAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        InstancesListWrapper wrapper = await GetAsync<InstancesListWrapper>(
            $"{DatabasesPath}/{databaseName}/instances", cancellationToken);
        return wrapper.Instances;
    }

    public async Task<DatabaseInstance> GetInstanceAsync(string databaseName, string instanceName, CancellationToken cancellationToken = default)
    {
        InstanceWrapper wrapper = await GetAsync<InstanceWrapper>(
            $"{DatabasesPath}/{databaseName}/instances/{instanceName}", cancellationToken);
        return wrapper.Instance;
    }

    public async Task<DatabaseConfiguration> GetConfigurationAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DatabaseConfiguration>($"{DatabasesPath}/{databaseName}/configuration", cancellationToken);
    }

    public async Task<DatabaseConfiguration> UpdateConfigurationAsync(string databaseName, UpdateDatabaseConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<DatabaseConfiguration>($"{DatabasesPath}/{databaseName}/configuration", request, cancellationToken);
    }

    public async Task<DatabaseUsage> GetUsageAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        UsageWrapper wrapper = await GetAsync<UsageWrapper>($"{DatabasesPath}/{databaseName}/usage", cancellationToken);
        return wrapper.Database;
    }

    public async Task<Database> UploadDumpAsync(string databaseName, Stream dumpFile, CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent content = new();
        content.Add(new StreamContent(dumpFile), "file", "dump.sql");
        using HttpResponseMessage response = await HttpClient.PostAsync(
            $"{DatabasesPath}/{databaseName}/upload", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        DatabaseWrapper wrapper = (await response.Content.ReadFromJsonAsync<DatabaseWrapper>(JsonOptions, cancellationToken))!;
        return wrapper.Database;
    }

    private sealed class DatabasesListWrapper
    {
        [JsonPropertyName("databases")]
        public List<Database> Databases { get; set; } = [];
    }

    private sealed class DatabaseWrapper
    {
        [JsonPropertyName("database")]
        public Database Database { get; set; } = null!;
    }

    private sealed class DeleteWrapper
    {
        [JsonPropertyName("database")]
        public string Database { get; set; } = string.Empty;
    }

    private sealed class InstancesListWrapper
    {
        [JsonPropertyName("instances")]
        public List<DatabaseInstance> Instances { get; set; } = [];
    }

    private sealed class InstanceWrapper
    {
        [JsonPropertyName("instance")]
        public DatabaseInstance Instance { get; set; } = null!;
    }

    private sealed class UsageWrapper
    {
        [JsonPropertyName("database")]
        public DatabaseUsage Database { get; set; } = null!;
    }
}

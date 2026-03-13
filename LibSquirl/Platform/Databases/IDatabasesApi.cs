using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Databases;

public interface IDatabasesApi
{
    Task<List<Database>> ListAsync(CancellationToken cancellationToken = default);
    Task<Database> CreateAsync(
        CreateDatabaseRequest request,
        CancellationToken cancellationToken = default
    );
    Task<Database> GetAsync(string databaseName, CancellationToken cancellationToken = default);
    Task<string> DeleteAsync(string databaseName, CancellationToken cancellationToken = default);

    Task<TokenResponse> CreateTokenAsync(
        string databaseName,
        string? expiration = null,
        string? authorization = null,
        CreateTokenRequest? body = null,
        CancellationToken cancellationToken = default
    );

    Task InvalidateTokensAsync(string databaseName, CancellationToken cancellationToken = default);
    Task<DatabaseStats> GetStatsAsync(
        string databaseName,
        CancellationToken cancellationToken = default
    );
    Task<List<DatabaseInstance>> ListInstancesAsync(
        string databaseName,
        CancellationToken cancellationToken = default
    );

    Task<DatabaseInstance> GetInstanceAsync(
        string databaseName,
        string instanceName,
        CancellationToken cancellationToken = default
    );

    Task<DatabaseConfiguration> GetConfigurationAsync(
        string databaseName,
        CancellationToken cancellationToken = default
    );

    Task<DatabaseConfiguration> UpdateConfigurationAsync(
        string databaseName,
        UpdateDatabaseConfigurationRequest request,
        CancellationToken cancellationToken = default
    );

    Task<DatabaseUsage> GetUsageAsync(
        string databaseName,
        CancellationToken cancellationToken = default
    );
    Task<Database> UploadDumpAsync(
        string databaseName,
        Stream dumpFile,
        CancellationToken cancellationToken = default
    );
}

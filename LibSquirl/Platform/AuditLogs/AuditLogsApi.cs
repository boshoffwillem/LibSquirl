using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.AuditLogs;

public sealed class AuditLogsApi(HttpClient httpClient, TursoPlatformOptions options)
    : PlatformApiBase(httpClient, options), IAuditLogsApi
{
    public async Task<AuditLogsResponse> ListAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await GetAsync<AuditLogsResponse>(
            $"{OrgPath}/audit-logs?page={page}&page_size={pageSize}", cancellationToken);
    }
}

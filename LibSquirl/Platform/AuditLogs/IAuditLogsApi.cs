using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.AuditLogs;

public interface IAuditLogsApi
{
    Task<AuditLogsResponse> ListAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}

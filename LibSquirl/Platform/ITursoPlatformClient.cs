using LibSquirl.Platform.AuditLogs;
using LibSquirl.Platform.Databases;
using LibSquirl.Platform.Groups;
using LibSquirl.Platform.Locations;
using LibSquirl.Platform.Organizations;
using LibSquirl.Platform.Tokens;

namespace LibSquirl.Platform;

public interface ITursoPlatformClient
{
    IOrganizationsApi Organizations { get; }
    IGroupsApi Groups { get; }
    IDatabasesApi Databases { get; }
    ILocationsApi Locations { get; }
    IApiTokensApi Tokens { get; }
    IAuditLogsApi AuditLogs { get; }
}

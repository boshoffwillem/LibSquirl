using LibSquirl.Platform.AuditLogs;
using LibSquirl.Platform.Databases;
using LibSquirl.Platform.Groups;
using LibSquirl.Platform.Locations;
using LibSquirl.Platform.Organizations;
using LibSquirl.Platform.Tokens;

namespace LibSquirl.Platform;

public sealed class TursoPlatformClient : ITursoPlatformClient
{
    public IOrganizationsApi Organizations { get; }
    public IGroupsApi Groups { get; }
    public IDatabasesApi Databases { get; }
    public ILocationsApi Locations { get; }
    public IApiTokensApi Tokens { get; }
    public IAuditLogsApi AuditLogs { get; }

    public TursoPlatformClient(HttpClient httpClient, TursoPlatformOptions options)
    {
        string baseUrl = options.BaseUrl.TrimEnd('/');
        httpClient.BaseAddress = new Uri(baseUrl);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiToken);

        Organizations = new OrganizationsApi(httpClient, options);
        Groups = new GroupsApi(httpClient, options);
        Databases = new DatabasesApi(httpClient, options);
        Locations = new LocationsApi(httpClient, options);
        Tokens = new ApiTokensApi(httpClient, options);
        AuditLogs = new AuditLogsApi(httpClient, options);
    }
}

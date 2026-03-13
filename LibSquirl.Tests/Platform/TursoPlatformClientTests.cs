using System.Net;

using LibSquirl.Platform;
using LibSquirl.Platform.AuditLogs;
using LibSquirl.Platform.Databases;
using LibSquirl.Platform.Groups;
using LibSquirl.Platform.Locations;
using LibSquirl.Platform.Organizations;
using LibSquirl.Platform.Tokens;

namespace LibSquirl.Tests.Platform;

public class TursoPlatformClientTests
{
    [Fact]
    public void Constructor_InitializesAllSubApis()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = "test-org"
        };

        TursoPlatformClient client = new(httpClient, options);

        Assert.NotNull(client.Organizations);
        Assert.NotNull(client.Groups);
        Assert.NotNull(client.Databases);
        Assert.NotNull(client.Locations);
        Assert.NotNull(client.Tokens);
        Assert.NotNull(client.AuditLogs);

        Assert.IsType<OrganizationsApi>(client.Organizations);
        Assert.IsType<GroupsApi>(client.Groups);
        Assert.IsType<DatabasesApi>(client.Databases);
        Assert.IsType<LocationsApi>(client.Locations);
        Assert.IsType<ApiTokensApi>(client.Tokens);
        Assert.IsType<AuditLogsApi>(client.AuditLogs);
    }

    [Fact]
    public void Constructor_SetsBaseAddress()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = "test-org",
            BaseUrl = "https://custom-api.example.com"
        };

        _ = new TursoPlatformClient(httpClient, options);

        Assert.Equal(new Uri("https://custom-api.example.com"), httpClient.BaseAddress);
    }

    [Fact]
    public void Constructor_SetsAuthorizationHeader()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        TursoPlatformOptions options = new()
        {
            ApiToken = "my-secret-token",
            OrganizationSlug = "test-org"
        };

        _ = new TursoPlatformClient(httpClient, options);

        Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("my-secret-token", httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }
}

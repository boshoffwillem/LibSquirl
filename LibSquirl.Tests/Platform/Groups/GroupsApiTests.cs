using System.Net;
using System.Text.Json;

using LibSquirl.Platform;
using LibSquirl.Platform.Groups;
using LibSquirl.Platform.Models;

namespace LibSquirl.Tests.Platform.Groups;

public class GroupsApiTests
{
    private const string OrgSlug = "test-org";

    private static (GroupsApi api, MockHttpMessageHandler handler) CreateApi()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.turso.tech") };
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = OrgSlug
        };
        return (new GroupsApi(httpClient, options), handler);
    }

    private const string GroupJson = """
        {"name":"default","version":"v0.23.7","uuid":"abc-123","locations":["aws-us-east-1"],"primary":"us-east-1","delete_protection":false}
    """;

    [Fact]
    public async Task ListAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"groups":[{{{GroupJson}}}]}""");

        List<Group> result = await api.ListAsync();

        Assert.Single(result);
        Assert.Equal("default", result[0].Name);
        Assert.Equal("v0.23.7", result[0].Version);
        Assert.Contains("aws-us-east-1", result[0].Locations);

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task CreateAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"group":{{{GroupJson}}}}""");

        Group result = await api.CreateAsync(new CreateGroupRequest
        {
            Name = "my-group",
            Location = "aws-us-east-1"
        });

        Assert.Equal("default", result.Name);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups", handler.Requests[0].Uri.AbsolutePath);

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("my-group", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("aws-us-east-1", body.RootElement.GetProperty("location").GetString());
    }

    [Fact]
    public async Task GetAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"group":{{{GroupJson}}}}""");

        Group result = await api.GetAsync("default");

        Assert.Equal("default", result.Name);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task DeleteAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"group":{{{GroupJson}}}}""");

        Group result = await api.DeleteAsync("default");

        Assert.Equal("default", result.Name);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task AddLocationAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"group":{{{GroupJson}}}}""");

        await api.AddLocationAsync("default", "aws-eu-west-1");

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/locations/aws-eu-west-1",
            handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task RemoveLocationAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"group":{{{GroupJson}}}}""");

        await api.RemoveLocationAsync("default", "aws-eu-west-1");

        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/locations/aws-eu-west-1",
            handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task CreateTokenAsync_WithParams_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"jwt":"token-value"}""");

        TokenResponse result = await api.CreateTokenAsync(
            "default", expiration: "2w", authorization: "read-only");

        Assert.Equal("token-value", result.Jwt);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("/auth/tokens", handler.Requests[0].Uri.AbsolutePath);
        Assert.Contains("expiration=2w", handler.Requests[0].Uri.Query);
        Assert.Contains("authorization=read-only", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task InvalidateTokensAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        await api.InvalidateTokensAsync("default");

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/auth/rotate",
            handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ListAsync_NotFound_ThrowsException()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.NotFound, """{"error":"not found"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(
            () => api.ListAsync());
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetConfigurationAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"heartbeat_url":"https://example.com/hb","allow_attach":true}""");

        var result = await api.GetConfigurationAsync("default");

        Assert.Equal("https://example.com/hb", result.HeartbeatUrl);
        Assert.True(result.AllowAttach);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/configuration", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"heartbeat_url":"https://example.com/hb2","allow_attach":false}""");

        var result = await api.UpdateConfigurationAsync("default", new() { HeartbeatUrl = "https://example.com/hb2" });

        Assert.Equal("https://example.com/hb2", result.HeartbeatUrl);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/configuration", handler.Requests[0].Uri.AbsolutePath);

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("https://example.com/hb2", body.RootElement.GetProperty("heartbeat_url").GetString());
    }

    [Fact]
    public async Task TransferAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        await api.TransferAsync("default", new() { Organization = "other-org" });

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/transfer", handler.Requests[0].Uri.AbsolutePath);

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("other-org", body.RootElement.GetProperty("organization").GetString());
    }

    [Fact]
    public async Task UnarchiveAsync_SendsCorrectRequest()
    {
        (GroupsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        await api.UnarchiveAsync("default");

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/groups/default/unarchive", handler.Requests[0].Uri.AbsolutePath);
    }
}

using System.Net;

using LibSquirl.Platform;
using LibSquirl.Platform.Locations;
using LibSquirl.Platform.Models;

namespace LibSquirl.Tests.Platform.Locations;

public class LocationsApiTests
{
    private static (LocationsApi api, MockHttpMessageHandler handler, MockHttpMessageHandler regionHandler) CreateApi()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.turso.tech") };
        MockHttpMessageHandler regionHandler = new();
        HttpClient regionHttpClient = new(regionHandler) { BaseAddress = new Uri("https://region.turso.io") };
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = "test-org"
        };
        return (new LocationsApi(httpClient, options, regionHttpClient), handler, regionHandler);
    }

    [Fact]
    public async Task ListAsync_SendsCorrectRequest()
    {
        (LocationsApi api, MockHttpMessageHandler handler, _) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"locations":{"aws-us-east-1":"AWS US East (Virginia)","aws-eu-west-1":"AWS EU West (Ireland)"}}
        """);

        Dictionary<string, string> result = await api.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("AWS US East (Virginia)", result["aws-us-east-1"]);
        Assert.Equal("AWS EU West (Ireland)", result["aws-eu-west-1"]);

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/v1/locations", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetClosestAsync_SendsCorrectRequest()
    {
        (LocationsApi api, _, MockHttpMessageHandler regionHandler) = CreateApi();
        regionHandler.EnqueueResponse(HttpStatusCode.OK, """{"server":"lhr","client":"lhr"}""");

        ClosestRegion result = await api.GetClosestAsync();

        Assert.Equal("lhr", result.Server);
        Assert.Equal("lhr", result.Client);
        Assert.Equal(HttpMethod.Get, regionHandler.Requests[0].Method);
    }

    [Fact]
    public async Task ListAsync_Unauthorized_ThrowsException()
    {
        (LocationsApi api, MockHttpMessageHandler handler, _) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized, """{"error":"invalid token"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(
            () => api.ListAsync());
        Assert.Equal(401, ex.StatusCode);
    }
}

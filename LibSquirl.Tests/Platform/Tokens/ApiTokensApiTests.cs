using System.Net;
using LibSquirl.Platform;
using LibSquirl.Platform.Models;
using LibSquirl.Platform.Tokens;

namespace LibSquirl.Tests.Platform.Tokens;

public class ApiTokensApiTests
{
    private static (ApiTokensApi api, MockHttpMessageHandler handler) CreateApi()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.turso.tech") };
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = "test-org",
        };
        return (new ApiTokensApi(httpClient, options), handler);
    }

    [Fact]
    public async Task ListAsync_SendsCorrectRequest()
    {
        (ApiTokensApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"tokens":[{"name":"my-token","id":"tok-123"}]}
            """
        );

        List<ApiToken> result = await api.ListAsync();

        Assert.Single(result);
        Assert.Equal("my-token", result[0].Name);
        Assert.Equal("tok-123", result[0].Id);

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/v1/auth/api-tokens", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task CreateAsync_SendsCorrectRequest()
    {
        (ApiTokensApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"name":"new-token","id":"tok-456","token":"jwt-value"}
            """
        );

        ApiToken result = await api.CreateAsync("new-token");

        Assert.Equal("new-token", result.Name);
        Assert.Equal("jwt-value", result.Token);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/v1/auth/api-tokens/new-token", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ValidateAsync_SendsCorrectRequest()
    {
        (ApiTokensApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"exp":-1}""");

        await api.ValidateAsync();

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/v1/auth/validate", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task RevokeAsync_SendsCorrectRequest()
    {
        (ApiTokensApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"token":"revoked-token"}""");

        await api.RevokeAsync("old-token");

        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal("/v1/auth/api-tokens/old-token", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ValidateAsync_InvalidToken_ThrowsException()
    {
        (ApiTokensApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized, """{"error":"invalid token"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(() =>
            api.ValidateAsync()
        );
        Assert.Equal(401, ex.StatusCode);
    }
}

using System.Net;
using LibSquirl.Platform;
using LibSquirl.Platform.AuditLogs;
using LibSquirl.Platform.Models;

namespace LibSquirl.Tests.Platform.AuditLogs;

public class AuditLogsApiTests
{
    private const string OrgSlug = "test-org";

    private static (AuditLogsApi api, MockHttpMessageHandler handler) CreateApi()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.turso.tech") };
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = OrgSlug,
        };
        return (new AuditLogsApi(httpClient, options), handler);
    }

    [Fact]
    public async Task ListAsync_SendsCorrectRequest()
    {
        (AuditLogsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {
                    "audit_logs": [
                        {"code":"db-create","message":"","origin":"cli","author":"iku","created_at":"2023-12-20T09:46:08Z","data":{}}
                    ],
                    "pagination": {"page":1,"page_size":10,"total_pages":1,"total_rows":1}
                }
            """
        );

        AuditLogsResponse result = await api.ListAsync();

        Assert.Single(result.AuditLogs);
        Assert.Equal("db-create", result.AuditLogs[0].Code);
        Assert.Equal("cli", result.AuditLogs[0].Origin);
        Assert.Equal("iku", result.AuditLogs[0].Author);
        Assert.Equal("2023-12-20T09:46:08Z", result.AuditLogs[0].CreatedAt);

        Assert.Equal(1, result.Pagination.Page);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.Equal(1, result.Pagination.TotalPages);
        Assert.Equal(1, result.Pagination.TotalRows);

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains(
            $"/v1/organizations/{OrgSlug}/audit-logs",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task ListAsync_WithPagination_SendsCorrectQueryParams()
    {
        (AuditLogsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"audit_logs":[],"pagination":{"page":2,"page_size":25,"total_pages":3,"total_rows":75}}
            """
        );

        await api.ListAsync(2, 25);

        Assert.Contains("page=2", handler.Requests[0].Uri.Query);
        Assert.Contains("page_size=25", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task ListAsync_EmptyLogs_ReturnsEmptyList()
    {
        (AuditLogsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"audit_logs":[],"pagination":{"page":1,"page_size":10,"total_pages":0,"total_rows":0}}
            """
        );

        AuditLogsResponse result = await api.ListAsync();

        Assert.Empty(result.AuditLogs);
        Assert.Equal(0, result.Pagination.TotalRows);
    }

    [Fact]
    public async Task ListAsync_Unauthorized_ThrowsException()
    {
        (AuditLogsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized, """{"error":"unauthorized"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(() =>
            api.ListAsync()
        );
        Assert.Equal(401, ex.StatusCode);
    }
}

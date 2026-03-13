using System.Net;
using System.Text.Json;

using LibSquirl.Platform;
using LibSquirl.Platform.Models;
using LibSquirl.Platform.Organizations;

namespace LibSquirl.Tests.Platform.Organizations;

public class OrganizationsApiTests
{
    private const string OrgSlug = "test-org";

    private static (OrganizationsApi api, MockHttpMessageHandler handler) CreateApi()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.turso.tech") };
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = OrgSlug
        };
        return (new OrganizationsApi(httpClient, options), handler);
    }

    [Fact]
    public async Task ListAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            [{"name":"personal","slug":"testuser","type":"personal","overages":false,"blocked_reads":false,"blocked_writes":false}]
        """);

        var result = await api.ListAsync();

        Assert.Single(result);
        Assert.Equal("personal", result[0].Name);
        Assert.Equal("testuser", result[0].Slug);

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/v1/organizations", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"organization":{"name":"test-org","slug":"test-org","type":"team","overages":false,"blocked_reads":false,"blocked_writes":false}}
        """);

        var result = await api.GetAsync();

        Assert.Equal("test-org", result.Name);
        Assert.Equal("team", result.Type);

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task UpdateAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"organization":{"name":"test-org","slug":"test-org","type":"team","overages":true,"blocked_reads":false,"blocked_writes":false}}
        """);

        var result = await api.UpdateAsync(new UpdateOrganizationRequest { Overages = true });

        Assert.True(result.Overages);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ListPlansAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"plans":[{"name":"starter","price":"$0","quotas":{"row_reads":1000000,"row_writes":500000,"databases":10,"locations":3,"storage":9000000000,"groups":3,"bytes_synced":0}}]}
        """);

        var result = await api.ListPlansAsync();

        Assert.Single(result);
        Assert.Equal("starter", result[0].Name);
        Assert.Equal("$0", result[0].Price);
        Assert.Equal(1000000, result[0].Quotas!.RowReads);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/plans", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetSubscriptionAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"subscription":{"subscription":"sub-123","plan":"starter","timeline":"monthly","overages":false}}
        """);

        var result = await api.GetSubscriptionAsync();

        Assert.Equal("sub-123", result.SubscriptionId);
        Assert.Equal("starter", result.Plan);
        Assert.Equal("monthly", result.Timeline);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/subscription", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ListInvoicesAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"invoices":[{"invoice_number":"INV-001","amount_due":"$10.00","due_date":"2024-01-01","paid_at":"2024-01-01"}]}
        """);

        var result = await api.ListInvoicesAsync();

        Assert.Single(result);
        Assert.Equal("INV-001", result[0].InvoiceNumber);
        Assert.Equal("$10.00", result[0].AmountDue);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/invoices", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetUsageAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"organization":{"uuid":"org-uuid","usage":{"rows_read":5000,"rows_written":1000,"storage_bytes":8192,"databases":3,"locations":2,"groups":1,"bytes_synced":512}}}
        """);

        var result = await api.GetUsageAsync();

        Assert.Equal("org-uuid", result.Uuid);
        Assert.Equal(5000, result.Usage.RowsRead);
        Assert.Equal(1000, result.Usage.RowsWritten);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/usage", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ListMembersAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"members":[{"username":"alice","role":"owner","email":"alice@example.com"},{"username":"bob","role":"member"}]}
        """);

        var result = await api.ListMembersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("alice", result[0].Username);
        Assert.Equal("owner", result[0].Role);
        Assert.Equal("alice@example.com", result[0].Email);
        Assert.Equal("bob", result[1].Username);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/members", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetMemberAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"member":{"username":"alice","role":"owner","email":"alice@example.com"}}
        """);

        var result = await api.GetMemberAsync("alice");

        Assert.Equal("alice", result.Username);
        Assert.Equal("owner", result.Role);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/members/alice", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task AddMemberAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"member":{"username":"charlie","role":"member"}}
        """);

        var result = await api.AddMemberAsync(new AddMemberRequest { Username = "charlie", Role = "member" });

        Assert.Equal("charlie", result.Username);
        Assert.Equal("member", result.Role);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/members", handler.Requests[0].Uri.AbsolutePath);

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("charlie", body.RootElement.GetProperty("username").GetString());
        Assert.Equal("member", body.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task UpdateMemberAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"member":{"username":"charlie","role":"admin"}}
        """);

        var result = await api.UpdateMemberAsync("charlie", new UpdateMemberRequest { Role = "admin" });

        Assert.Equal("admin", result.Role);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/members/charlie", handler.Requests[0].Uri.AbsolutePath);

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("admin", body.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task RemoveMemberAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK);

        await api.RemoveMemberAsync("charlie");

        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/members/charlie", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task ListInvitesAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"invites":[{"email":"dave@example.com","role":"member","accepted":false,"created_at":"2024-01-01T00:00:00Z"}]}
        """);

        var result = await api.ListInvitesAsync();

        Assert.Single(result);
        Assert.Equal("dave@example.com", result[0].Email);
        Assert.Equal("member", result[0].Role);
        Assert.False(result[0].Accepted);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/invites", handler.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task CreateInviteAsync_SendsCorrectRequest()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """
            {"invited":{"email":"eve@example.com","role":"member","accepted":false}}
        """);

        var result = await api.CreateInviteAsync(new CreateInviteRequest { Email = "eve@example.com", Role = "member" });

        Assert.Equal("eve@example.com", result.Email);
        Assert.Equal("member", result.Role);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/v1/organizations/{OrgSlug}/invites", handler.Requests[0].Uri.AbsolutePath);

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("eve@example.com", body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task ListAsync_ServerError_ThrowsException()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.InternalServerError, """{"error":"internal error"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(
            () => api.ListAsync());
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task GetAsync_Unauthorized_ThrowsException()
    {
        (OrganizationsApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.Unauthorized, """{"error":"unauthorized"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(
            () => api.GetAsync());
        Assert.Equal(401, ex.StatusCode);
    }
}

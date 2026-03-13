using System.Net;
using System.Text;
using System.Text.Json;
using LibSquirl.Platform;
using LibSquirl.Platform.Databases;
using LibSquirl.Platform.Models;

namespace LibSquirl.Tests.Platform.Databases;

public class DatabasesApiTests
{
    private const string OrgSlug = "test-org";

    private const string DbJson = """
            {"Name":"my-db","DbId":"db-123","Hostname":"my-db-test-org.turso.io","block_reads":false,"block_writes":false,"regions":["aws-us-east-1"],"primaryRegion":"aws-us-east-1","group":"default","delete_protection":false}
        """;

    private static (DatabasesApi api, MockHttpMessageHandler handler) CreateApi()
    {
        MockHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.turso.tech") };
        TursoPlatformOptions options = new()
        {
            ApiToken = "test-token",
            OrganizationSlug = OrgSlug,
        };
        return (new DatabasesApi(httpClient, options), handler);
    }

    [Fact]
    public async Task ListAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"databases":[{{{DbJson}}}]}""");

        List<Database> result = await api.ListAsync();

        Assert.Single(result);
        Assert.Equal("my-db", result[0].Name);
        Assert.Equal("db-123", result[0].DbId);
        Assert.Equal("my-db-test-org.turso.io", result[0].Hostname);

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task CreateAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"database":{{{DbJson}}}}""");

        Database result = await api.CreateAsync(
            new CreateDatabaseRequest { Name = "my-db", Group = "default" }
        );

        Assert.Equal("my-db", result.Name);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases",
            handler.Requests[0].Uri.AbsolutePath
        );

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("my-db", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("default", body.RootElement.GetProperty("group").GetString());
    }

    [Fact]
    public async Task CreateAsync_WithSeed_SerializesSeed()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"database":{{{DbJson}}}}""");

        await api.CreateAsync(
            new CreateDatabaseRequest
            {
                Name = "my-db",
                Group = "default",
                Seed = new DatabaseSeed { Type = "database", Name = "source-db" },
                SizeLimit = "256mb",
            }
        );

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal(
            "database",
            body.RootElement.GetProperty("seed").GetProperty("type").GetString()
        );
        Assert.Equal(
            "source-db",
            body.RootElement.GetProperty("seed").GetProperty("name").GetString()
        );
        Assert.Equal("256mb", body.RootElement.GetProperty("size_limit").GetString());
    }

    [Fact]
    public async Task GetAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"database":{{{DbJson}}}}""");

        Database result = await api.GetAsync("my-db");

        Assert.Equal("my-db", result.Name);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task DeleteAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"database":"my-db"}""");

        string result = await api.DeleteAsync("my-db");

        Assert.Equal("my-db", result);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task CreateTokenAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, """{"jwt":"db-token"}""");

        TokenResponse result = await api.CreateTokenAsync("my-db", "1d", "full-access");

        Assert.Equal("db-token", result.Jwt);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("/databases/my-db/auth/tokens", handler.Requests[0].Uri.AbsolutePath);
        Assert.Contains("expiration=1d", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task InvalidateTokensAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        await api.InvalidateTokensAsync("my-db");

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/auth/rotate",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task GetStatsAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"rows_read":100,"rows_written":50,"storage_bytes":1024}
            """
        );

        DatabaseStats result = await api.GetStatsAsync("my-db");

        Assert.Equal(100, result.RowsRead);
        Assert.Equal(50, result.RowsWritten);
        Assert.Equal(1024, result.StorageBytes);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/stats",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task ListInstancesAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"instances":[{"uuid":"inst-1","name":"primary","type":"primary","region":"aws-us-east-1","hostname":"host.turso.io"}]}
            """
        );

        List<DatabaseInstance> result = await api.ListInstancesAsync("my-db");

        Assert.Single(result);
        Assert.Equal("inst-1", result[0].Uuid);
        Assert.Equal("primary", result[0].Name);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/instances",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task CreateAsync_Conflict_ThrowsException()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.Conflict, """{"error":"database already exists"}""");

        TursoPlatformException ex = await Assert.ThrowsAsync<TursoPlatformException>(() =>
            api.CreateAsync(new CreateDatabaseRequest { Name = "existing", Group = "default" })
        );
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task GetInstanceAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"instance":{"uuid":"inst-1","name":"primary","type":"primary","region":"aws-us-east-1","hostname":"host.turso.io"}}
            """
        );

        DatabaseInstance result = await api.GetInstanceAsync("my-db", "primary");

        Assert.Equal("inst-1", result.Uuid);
        Assert.Equal("primary", result.Name);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/instances/primary",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task GetConfigurationAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"size_limit":"256mb","allow_attach":true,"block_reads":false,"block_writes":false}
            """
        );

        DatabaseConfiguration result = await api.GetConfigurationAsync("my-db");

        Assert.Equal("256mb", result.SizeLimit);
        Assert.True(result.AllowAttach);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/configuration",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task UpdateConfigurationAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"size_limit":"512mb","allow_attach":false,"block_reads":false,"block_writes":true}
            """
        );

        DatabaseConfiguration result = await api.UpdateConfigurationAsync(
            "my-db",
            new UpdateDatabaseConfigurationRequest { SizeLimit = "512mb", BlockWrites = true }
        );

        Assert.Equal("512mb", result.SizeLimit);
        Assert.True(result.BlockWrites);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/configuration",
            handler.Requests[0].Uri.AbsolutePath
        );

        JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("512mb", body.RootElement.GetProperty("size_limit").GetString());
    }

    [Fact]
    public async Task GetUsageAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(
            HttpStatusCode.OK,
            """
                {"database":{"rows_read":500,"rows_written":200,"storage_bytes":4096,"bytes_synced":1024}}
            """
        );

        DatabaseUsage result = await api.GetUsageAsync("my-db");

        Assert.Equal(500, result.RowsRead);
        Assert.Equal(200, result.RowsWritten);
        Assert.Equal(4096, result.StorageBytes);
        Assert.Equal(1024, result.BytesSynced);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/usage",
            handler.Requests[0].Uri.AbsolutePath
        );
    }

    [Fact]
    public async Task UploadDumpAsync_SendsCorrectRequest()
    {
        (DatabasesApi api, MockHttpMessageHandler handler) = CreateApi();
        handler.EnqueueResponse(HttpStatusCode.OK, $$$"""{"database":{{{DbJson}}}}""");

        using MemoryStream stream = new(Encoding.UTF8.GetBytes("CREATE TABLE test (id INT);"));
        Database result = await api.UploadDumpAsync("my-db", stream);

        Assert.Equal("my-db", result.Name);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal(
            $"/v1/organizations/{OrgSlug}/databases/my-db/upload",
            handler.Requests[0].Uri.AbsolutePath
        );
    }
}

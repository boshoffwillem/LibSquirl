using System.Net;
using System.Text;

using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

public class LibSqlClientMigrationJobsTests
{
    private static ILibSqlClient CreateClient(MockHttpHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:8080") };
        return new LibSqlClient(httpClient, new LibSqlClientOptions { Url = "http://localhost:8080" });
    }

    [Fact]
    public async Task ListMigrationJobsAsync_DeserializesCorrectly()
    {
        MockHttpHandler handler = new("""
            {"schema_version":2,"migrations":[{"job_id":1,"status":"RunSuccess"},{"job_id":2,"status":"RunFailure"}]}
        """);

        ILibSqlClient client = CreateClient(handler);
        MigrationJobsSummary result = await client.ListMigrationJobsAsync();

        Assert.Equal(2, result.SchemaVersion);
        Assert.Equal(2, result.Migrations.Count);
        Assert.Equal(1, result.Migrations[0].JobId);
        Assert.Equal("RunSuccess", result.Migrations[0].Status);
        Assert.Equal(2, result.Migrations[1].JobId);
        Assert.Equal("RunFailure", result.Migrations[1].Status);

        Assert.Equal("/v1/jobs", handler.LastRequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.LastMethod);
    }

    [Fact]
    public async Task GetMigrationJobAsync_DeserializesCorrectly()
    {
        MockHttpHandler handler = new("""
            {"job_id":1,"status":"RunSuccess","error":null,"progress":[{"namespace":"default","status":"RunSuccess","error":null}]}
        """);

        ILibSqlClient client = CreateClient(handler);
        MigrationJobDetail result = await client.GetMigrationJobAsync(1);

        Assert.Equal(1, result.JobId);
        Assert.Equal("RunSuccess", result.Status);
        Assert.Null(result.Error);
        Assert.Single(result.Progress);
        Assert.Equal("default", result.Progress[0].Namespace);
        Assert.Equal("RunSuccess", result.Progress[0].Status);

        Assert.Equal("/v1/jobs/1", handler.LastRequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetMigrationJobAsync_WithError_DeserializesError()
    {
        MockHttpHandler handler = new("""
            {"job_id":3,"status":"RunFailure","error":"schema migration failed","progress":[{"namespace":"default","status":"RunFailure","error":"syntax error"}]}
        """);

        ILibSqlClient client = CreateClient(handler);
        MigrationJobDetail result = await client.GetMigrationJobAsync(3);

        Assert.Equal(3, result.JobId);
        Assert.Equal("RunFailure", result.Status);
        Assert.Equal("schema migration failed", result.Error);
        Assert.Equal("syntax error", result.Progress[0].Error);
    }

    /// Simple handler that always returns the same response for GET requests.
    private sealed class MockHttpHandler(string responseJson) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public HttpMethod? LastMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastMethod = request.Method;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}

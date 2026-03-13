using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Protocol;

public sealed class LibSqlClient : ILibSqlClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly LibSqlClientOptions _options;

    public LibSqlClient(HttpClient httpClient, LibSqlClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;

        string baseUrl = options.Url.TrimEnd('/');
        _httpClient.BaseAddress = new Uri(baseUrl);

        if (!string.IsNullOrEmpty(options.AuthToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                options.AuthToken
            );
        }
    }

    public async Task<StatementResult> ExecuteAsync(
        string sql,
        IReadOnlyList<Value>? args = null,
        IReadOnlyList<NamedArg>? namedArgs = null,
        CancellationToken cancellationToken = default
    )
    {
        Statement stmt = new() { Sql = sql };

        if (args is { Count: > 0 })
        {
            stmt.Args = [.. args];
        }

        if (namedArgs is { Count: > 0 })
        {
            stmt.NamedArgs = [.. namedArgs];
        }

        PipelineRequest request = new()
        {
            Requests = [StreamRequest.Execute(stmt), StreamRequest.Close()],
        };

        PipelineResponse response = await PipelineAsync(request, cancellationToken);
        StreamResult executeResult = response.Results[0];

        if (executeResult.IsError)
        {
            throw new LibSqlException(executeResult.Error!);
        }

        return ((ExecuteStreamResponse)executeResult.Response!).Result;
    }

    public async Task<BatchResult> BatchAsync(
        Batch batch,
        CancellationToken cancellationToken = default
    )
    {
        PipelineRequest request = new()
        {
            Requests = [StreamRequest.ExecuteBatch(batch), StreamRequest.Close()],
        };

        PipelineResponse response = await PipelineAsync(request, cancellationToken);
        StreamResult batchResult = response.Results[0];

        if (batchResult.IsError)
        {
            throw new LibSqlException(batchResult.Error!);
        }

        return ((BatchStreamResponse)batchResult.Response!).Result;
    }

    public async Task SequenceAsync(string sql, CancellationToken cancellationToken = default)
    {
        PipelineRequest request = new()
        {
            Requests = [StreamRequest.Sequence(sql), StreamRequest.Close()],
        };

        PipelineResponse response = await PipelineAsync(request, cancellationToken);
        StreamResult seqResult = response.Results[0];

        if (seqResult.IsError)
        {
            throw new LibSqlException(seqResult.Error!);
        }
    }

    public async Task<DescribeResult> DescribeAsync(
        string sql,
        CancellationToken cancellationToken = default
    )
    {
        PipelineRequest request = new()
        {
            Requests = [StreamRequest.Describe(sql), StreamRequest.Close()],
        };

        PipelineResponse response = await PipelineAsync(request, cancellationToken);
        StreamResult descResult = response.Results[0];

        if (descResult.IsError)
        {
            throw new LibSqlException(descResult.Error!);
        }

        return ((DescribeStreamResponse)descResult.Response!).Result;
    }

    public async Task<PipelineResponse> PipelineAsync(
        PipelineRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(
            "/v2/pipeline",
            request,
            s_jsonOptions,
            cancellationToken
        );

        if (!httpResponse.IsSuccessStatusCode)
        {
            string body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new LibSqlException(
                $"Pipeline request failed with status {httpResponse.StatusCode}: {body}"
            );
        }

        PipelineResponse? response = await httpResponse.Content.ReadFromJsonAsync<PipelineResponse>(
            s_jsonOptions,
            cancellationToken
        );

        return response ?? throw new LibSqlException("Received null pipeline response");
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                "/health",
                cancellationToken
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "/version",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> DumpAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("/dump", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<MigrationJobsSummary> ListMigrationJobsAsync(
        CancellationToken cancellationToken = default
    )
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "/v1/jobs",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<MigrationJobsSummary>(json, s_jsonOptions)
            ?? throw new LibSqlException("Failed to deserialize migration jobs response");
    }

    public async Task<MigrationJobDetail> GetMigrationJobAsync(
        int jobId,
        CancellationToken cancellationToken = default
    )
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            $"/v1/jobs/{jobId}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<MigrationJobDetail>(json, s_jsonOptions)
            ?? throw new LibSqlException("Failed to deserialize migration job detail response");
    }
}

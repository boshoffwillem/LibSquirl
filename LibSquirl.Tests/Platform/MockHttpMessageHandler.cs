using System.Net;
using System.Text;
using System.Text.Json;

namespace LibSquirl.Tests.Platform;

/// A mock HttpMessageHandler that records requests and returns preconfigured responses.
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<MockResponse> _responses = new();
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests => _requests;

    public void EnqueueResponse(HttpStatusCode statusCode, object? body = null)
    {
        string? json = body is not null ? JsonSerializer.Serialize(body) : null;
        _responses.Enqueue(new MockResponse(statusCode, json));
    }

    public void EnqueueResponse(HttpStatusCode statusCode, string json)
    {
        _responses.Enqueue(new MockResponse(statusCode, json));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? requestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        _requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!,
            requestBody));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException(
                $"No mock response configured for {request.Method} {request.RequestUri}");
        }

        MockResponse mockResponse = _responses.Dequeue();

        HttpResponseMessage response = new(mockResponse.StatusCode);
        if (mockResponse.Body is not null)
        {
            response.Content = new StringContent(mockResponse.Body, Encoding.UTF8, "application/json");
        }
        return response;
    }

    private sealed record MockResponse(HttpStatusCode StatusCode, string? Body);
}

public sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body);

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibSquirl.Platform;

public abstract class PlatformApiBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected readonly HttpClient HttpClient;
    protected readonly TursoPlatformOptions Options;

    protected PlatformApiBase(HttpClient httpClient, TursoPlatformOptions options)
    {
        HttpClient = httpClient;
        Options = options;
    }

    protected string OrgPath => $"/v1/organizations/{Options.OrganizationSlug}";

    protected async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))!;
    }

    protected async Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = body is not null
            ? await HttpClient.PostAsJsonAsync(path, body, JsonOptions, cancellationToken)
            : await HttpClient.PostAsync(path, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))!;
    }

    protected async Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.DeleteAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))!;
    }

    protected async Task DeleteNoContentAsync(string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.DeleteAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected async Task<T> PatchAsync<T>(string path, object? body, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = body is not null
            ? await HttpClient.PatchAsJsonAsync(path, body, JsonOptions, cancellationToken)
            : await HttpClient.PatchAsync(path, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))!;
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TursoPlatformException(
                $"API request failed with status {response.StatusCode}: {body}",
                (int)response.StatusCode);
        }
    }
}
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Locations;

public sealed class LocationsApi : PlatformApiBase, ILocationsApi
{
    private readonly HttpClient _regionHttpClient;

    public LocationsApi(HttpClient httpClient, TursoPlatformOptions options, HttpClient? regionHttpClient = null)
        : base(httpClient, options)
    {
        _regionHttpClient = regionHttpClient ?? new HttpClient { BaseAddress = new Uri("https://region.turso.io") };
    }

    public async Task<Dictionary<string, string>> ListAsync(CancellationToken cancellationToken = default)
    {
        LocationsWrapper wrapper = await GetAsync<LocationsWrapper>("/v1/locations", cancellationToken);
        return wrapper.Locations;
    }

    public async Task<ClosestRegion> GetClosestAsync(CancellationToken cancellationToken = default)
    {
        return (await _regionHttpClient.GetFromJsonAsync<ClosestRegion>("/", JsonOptions, cancellationToken))!;
    }

    private sealed class LocationsWrapper
    {
        [JsonPropertyName("locations")]
        public Dictionary<string, string> Locations { get; set; } = [];
    }
}

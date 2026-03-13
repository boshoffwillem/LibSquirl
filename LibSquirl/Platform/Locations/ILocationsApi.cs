using LibSquirl.Platform.Models;

namespace LibSquirl.Platform.Locations;

public interface ILocationsApi
{
    Task<Dictionary<string, string>> ListAsync(CancellationToken cancellationToken = default);
    Task<ClosestRegion> GetClosestAsync(CancellationToken cancellationToken = default);
}

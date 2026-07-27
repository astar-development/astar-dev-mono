using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AStarDev.Web.Packages;

/// <inheritdoc cref="INugetApiClient" />
public sealed class NugetApiClient(HttpClient httpClient) : INugetApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<PackageData?> FetchAsync(string packageId, CancellationToken cancellationToken)
    {
        var url = $"query?q=packageid:{Uri.EscapeDataString(packageId)}&prerelease=false&take=1";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<NugetSearchResponse>(SerializerOptions, cancellationToken);
        var entry = payload?.Data?.FirstOrDefault(d => string.Equals(d.Id, packageId, StringComparison.OrdinalIgnoreCase));

        return entry is null
            ? null
            : PackageDataFactory.Create(entry.Id ?? packageId, entry.Version ?? "", entry.Description ?? "", entry.TotalDownloads, entry.ProjectUrl ?? "");
    }

    private sealed class NugetSearchResponse
    {
        [JsonPropertyName("data")]
        public IReadOnlyList<NugetSearchResultEntry>? Data { get; init; }
    }

    private sealed class NugetSearchResultEntry
    {
        public string? Id { get; init; }

        public string? Version { get; init; }

        public string? Description { get; init; }

        public long TotalDownloads { get; init; }

        public string? ProjectUrl { get; init; }
    }
}

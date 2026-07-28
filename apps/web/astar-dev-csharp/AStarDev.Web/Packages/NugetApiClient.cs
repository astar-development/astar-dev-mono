using System.Text.Json;
using System.Text.Json.Serialization;
using AStar.Dev.FunctionalParadigm;

namespace AStarDev.Web.Packages;

/// <inheritdoc cref="INugetApiClient" />
public sealed class NugetApiClient(HttpClient httpClient) : INugetApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Option<PackageData>> FetchAsync(string packageId, CancellationToken cancellationToken)
    {
        var url = $"query?q=packageid:{Uri.EscapeDataString(packageId)}&prerelease=false&take=1";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Option.None<PackageData>();

        var payload = await response.Content.ReadFromJsonAsync<NugetSearchResponse>(SerializerOptions, cancellationToken);
        var entries = payload?.Data ?? [];
        var entry = entries.FirstOrNone(d => string.Equals(d.Id, packageId, StringComparison.OrdinalIgnoreCase));

        return entry.Map(e => PackageDataFactory.Create(e.Id ?? packageId, e.Version ?? "", e.Description ?? "", e.TotalDownloads, e.ProjectUrl ?? ""));
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

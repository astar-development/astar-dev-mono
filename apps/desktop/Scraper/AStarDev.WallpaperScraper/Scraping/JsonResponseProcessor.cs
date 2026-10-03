using System.Net.Http.Json;
using System.Text.Json;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.Scraping;

/// <inheritdoc/>
public sealed class JsonResponseProcessor : IJsonResponseProcessor
{
    private const int MaximumErrorBodyLength = 500;

    /// <inheritdoc/>
    public Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken)
        => Try.RunAsync<Option<T>>(() => RequestTimeouts.RunAsync<Option<T>>(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await ReadErrorBodyAsync(response.Content, cancellationToken);

                throw new HttpRequestException(
                    $"Wallhaven returned {(int)response.StatusCode} ({response.StatusCode}) for {url}. " +
                    $"Location: {response.Headers.Location}. Response: {responseBody}");
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException)
            {
                throw new InvalidOperationException($"Unable to deserialize response from {url} as {typeof(T).Name}.", exception);
            }
        }, cancellationToken), cancellationToken);

    private static async Task<string> ReadErrorBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var buffer = new char[MaximumErrorBodyLength + 1];
        var read = await reader.ReadBlockAsync(buffer.AsMemory(), cancellationToken);
        if (read <= MaximumErrorBodyLength) return new string(buffer, 0, read);

        var length = char.IsHighSurrogate(buffer[MaximumErrorBodyLength - 1]) ? MaximumErrorBodyLength - 1 : MaximumErrorBodyLength;

        return $"{new string(buffer, 0, length)}…";
    }
}

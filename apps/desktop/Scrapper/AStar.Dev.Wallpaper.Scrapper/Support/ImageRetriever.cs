using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>The production <see cref="IImageRetriever" />, backed by an injected <see cref="HttpClient" />.</summary>
public sealed class ImageRetriever(HttpClient httpClient) : IImageRetriever
{
    /// <inheritdoc />
    public async Task<Result<byte[], ScrapeError>> GetImageAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return ScrapeErrorFactory.CreateImageDownloadFailed(url, $"Received status code {(int)response.StatusCode} ({response.ReasonPhrase}).");

            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            return ScrapeErrorFactory.CreateImageDownloadFailed(url, exception.Message);
        }
    }
}

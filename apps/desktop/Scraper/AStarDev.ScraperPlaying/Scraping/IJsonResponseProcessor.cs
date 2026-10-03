using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for processing JSON responses by fetching and deserializing them asynchronously.</summary>
public interface IJsonResponseProcessor
{
    /// <summary>Fetches a JSON response from the specified URL and deserializes it into the specified type asynchronously.</summary>
    /// <typeparam name="T">The type to deserialize the JSON response into.</typeparam>
    /// <param name="url">The URL to fetch the JSON response from.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/>: a success
    /// wrapping an <see cref="Option{T}"/> of the deserialized object (absent only if the response was successful
    /// but its body deserializes to null), or a failure wrapping an <see cref="HttpRequestException"/> when the
    /// response status code does not indicate success, or an <see cref="InvalidOperationException"/> when the
    /// successful response body could not be deserialized as <typeparamref name="T"/>.
    /// </returns>
    Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken);
}
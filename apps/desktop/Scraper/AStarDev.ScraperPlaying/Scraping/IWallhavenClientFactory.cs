namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Creates the <see cref="HttpClient"/> used to talk to Wallhaven for a connection.</summary>
public interface IWallhavenClientFactory
{
    /// <summary>Creates a client aimed at <paramref name="connection"/>'s host and authenticated with its API key.</summary>
    /// <param name="connection">The Wallhaven API credentials and target host.</param>
    HttpClient Create(WallhavenConnection connection);
}

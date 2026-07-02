namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class ImageRetrieverHelper
{
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(2) };

    public static async Task<byte[]> GetTheImageAsync(string src)
    {
        var response = await _client.GetAsync(src);

        return response is { IsSuccessStatusCode: true, } ? await response.Content.ReadAsByteArrayAsync() : [];
    }
}

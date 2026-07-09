namespace AStar.Dev.Wallpaper.Scraper.Support;

public sealed class ImageBroadcaster
{
    public event Action<string>? ImageSaved;
    public void Broadcast(string imagePath) => ImageSaved?.Invoke(imagePath);
}

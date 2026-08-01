namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class LogBroadcaster
{
    public event Action<string>? MessageLogged;
    public void Broadcast(string message) => MessageLogged?.Invoke(message);
}

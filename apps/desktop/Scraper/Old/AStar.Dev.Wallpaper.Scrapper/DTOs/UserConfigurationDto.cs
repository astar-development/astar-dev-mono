namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public sealed record UserConfigurationDto
{
    public string LoginEmailAddress { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string SessionCookie { get; init; } = string.Empty;
}

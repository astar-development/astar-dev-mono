namespace AStarDev.WallpaperScraper.Services;

public abstract record PageResult;

public sealed record PageSuccess(string Content, Uri Url) : PageResult;

public sealed record PageFailure(string ErrorMessage, Uri Url, string StatusText) : PageResult;

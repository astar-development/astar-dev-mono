namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Whether a search visits only a limited batch of pages per run.</summary>
public abstract record PageBatching;

/// <summary>The search visits at most <see cref="ScrapeLimits.MaximumPagesPerRun"/> pages per run and resumes from its recorded progress next run.</summary>
public sealed record BatchedPaging : PageBatching;

/// <summary>The search is not limited by <see cref="ScrapeLimits.MaximumPagesPerRun"/>; it records no progress to resume from.</summary>
public sealed record UnbatchedPaging : PageBatching;

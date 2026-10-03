namespace AStarDev.ScraperPlaying.Downloads;

/// <summary>What clearing the downloads removed.</summary>
/// <param name="FileRecords">The number of file records removed from the database.</param>
public readonly record struct ClearedDownloads(int FileRecords);

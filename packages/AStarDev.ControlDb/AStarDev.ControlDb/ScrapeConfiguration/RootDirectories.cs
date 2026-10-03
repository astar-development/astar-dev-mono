namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>The two base directories scraped content is saved under.</summary>
/// <param name="Root">The base directory for scraped content.</param>
/// <param name="FamousRoot">The base directory for famous scraped content.</param>
public sealed record RootDirectories(string Root, string FamousRoot);

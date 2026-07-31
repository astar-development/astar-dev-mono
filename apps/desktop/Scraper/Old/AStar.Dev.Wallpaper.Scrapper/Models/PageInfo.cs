namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>The information parsed from a search-results-style page header.</summary>
/// <param name="PageCount">The total number of pages available.</param>
/// <param name="ImageCount">The total number of images reported by the header.</param>
/// <param name="SubDirectoryName">The sub-directory name derived from the header text.</param>
public record PageInfo(int PageCount, int ImageCount, string SubDirectoryName);

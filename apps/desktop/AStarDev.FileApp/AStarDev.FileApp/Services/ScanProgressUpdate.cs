namespace AStar.Dev.File.App.Services;

public record ScanProgressUpdate(string CurrentFolder, int TotalFilesProcessed, string? CurrentFileName, string StatusMessage);

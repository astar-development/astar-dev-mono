namespace AStar.Dev.File.App.Services;

public interface IFileScannerService
{
    Task ScanAsync(string rootPath, IProgress<ScanProgressUpdate> progress, CancellationToken ct);
}

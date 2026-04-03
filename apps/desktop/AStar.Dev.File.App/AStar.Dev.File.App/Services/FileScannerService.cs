using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AStar.Dev.File.App.Services;

public class FileScannerService(
    IDbContextFactory<FileAppDbContext> dbContextFactory,
    IFileTypeClassifier classifier) : IFileScannerService
{
    private const int ProgressReportInterval = 500;

    private sealed class Counter { public int Value; }

    public async Task ScanAsync(string rootPath, IProgress<ScanProgressUpdate> progress, CancellationToken ct)
    {
        var scanStartedAt = DateTime.UtcNow;
        var counter = new Counter();

        await RecurseDirectoryAsync(rootPath, rootPath, progress, counter, ct);

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        await db.ScannedFiles
            .Where(f => f.RootPath == rootPath && f.LastScannedAt < scanStartedAt)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.PendingDelete, true), ct);

        string time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        progress.Report(new ScanProgressUpdate(
            CurrentFolder: rootPath,
            TotalFilesProcessed: counter.Value,
            CurrentFileName: null,
            StatusMessage: $"[{time}] Scan complete. {counter.Value} files processed."));
    }

    private async Task RecurseDirectoryAsync(
        string rootPath,
        string directory,
        IProgress<ScanProgressUpdate> progress,
        Counter counter,
        CancellationToken ct)
    {
        string time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        progress.Report(new ScanProgressUpdate(CurrentFolder: directory, TotalFilesProcessed: counter.Value, CurrentFileName: null, StatusMessage: $"[{time}] Scanning: {directory}"));

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        try
        {
            foreach (string filePath in Directory.EnumerateFiles(directory))
            {
                ct.ThrowIfCancellationRequested();

                var fi = new FileInfo(filePath);
                var existing = await db.ScannedFiles
                    .FirstOrDefaultAsync(f => f.FullPath == fi.FullName, ct);

                var now = DateTime.UtcNow;

                if (existing is not null)
                {
                    existing.LastModified = fi.LastWriteTimeUtc;
                    existing.SizeInBytes = fi.Length;
                    existing.FileType = classifier.Classify(fi.Extension);
                    existing.LastScannedAt = now;
                    existing.PendingDelete = false;
                }
                else
                {
                    db.ScannedFiles.Add(new ScannedFile
                    {
                        RootPath = rootPath,
                        FolderPath = directory,
                        FileName = fi.Name,
                        FullPath = fi.FullName,
                        LastModified = fi.LastWriteTimeUtc,
                        SizeInBytes = fi.Length,
                        FileType = classifier.Classify(fi.Extension),
                        LastViewed = null,
                        PendingDelete = false,
                        LastScannedAt = now
                    });
                }

                counter.Value++;

                if (counter.Value % ProgressReportInterval == 0)
                {
                    time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
                    progress.Report(new ScanProgressUpdate(
                        CurrentFolder: directory,
                        TotalFilesProcessed: counter.Value,
                        CurrentFileName: fi.Name,
                        StatusMessage: $"[{time}] {counter.Value} files processed..."));
                }
            }

            await db.SaveChangesAsync(ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            string errTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            progress.Report(new ScanProgressUpdate(
                CurrentFolder: directory,
                TotalFilesProcessed: counter.Value,
                CurrentFileName: null,
                StatusMessage: $"[{errTime}] ACCESS DENIED: {ex.Message}"));
        }

        foreach (string subDir in Directory.EnumerateDirectories(directory))
        {
            ct.ThrowIfCancellationRequested();
            await RecurseDirectoryAsync(rootPath, subDir, progress, counter, ct);
        }
    }
}

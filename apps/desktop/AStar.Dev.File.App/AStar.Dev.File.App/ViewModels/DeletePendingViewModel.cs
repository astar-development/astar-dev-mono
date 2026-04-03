using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Reactive;

namespace AStar.Dev.File.App.ViewModels;

public class DeletePendingViewModel : ViewModelBase
{
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private readonly IFileDeleteService _fileDeleteService;
    private readonly IFileViewerService _fileViewerService;

    [Reactive] public bool IsDeleting { get; set; }
    [Reactive] public int PendingDeleteCount { get; set; }
    [Reactive] public string StatusMessage { get; set; } = string.Empty;

    public ObservableCollection<ScannedFileDisplayItem> PendingDeleteFiles { get; } = [];

    public event Action<ScannedFileDisplayItem>? ViewFileRequested;

    public ReactiveCommand<ScannedFileDisplayItem?, Unit> TogglePendingDeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteAllCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearMarkingsCommand { get; }
    public ReactiveCommand<ScannedFileDisplayItem?, Unit> ViewFileCommand { get; }

    public DeletePendingViewModel(IDbContextFactory<FileAppDbContext> dbContextFactory,IFileDeleteService fileDeleteService, IFileViewerService fileViewerService)
    {
        _dbContextFactory = dbContextFactory;
        _fileDeleteService = fileDeleteService;
        _fileViewerService = fileViewerService;
        _fileViewerService.FileViewRequested += item => ViewFileRequested?.Invoke(item);

        TogglePendingDeleteCommand = ReactiveCommand.CreateFromTask<ScannedFileDisplayItem?>(TogglePendingDeleteAsync);

        var canDeleteAll = this.WhenAnyValue(x => x.IsDeleting, x => x.PendingDeleteCount,
            (deleting, count) => !deleting && count > 0);
        DeleteAllCommand = ReactiveCommand.CreateFromTask(DeleteAllAsync, canDeleteAll);

        ClearMarkingsCommand = ReactiveCommand.CreateFromTask(ClearMarkingsAsync);
        ViewFileCommand = ReactiveCommand.CreateFromTask<ScannedFileDisplayItem?>(ViewFileAsync);

        _ = LoadPendingFilesAsync();
    }

    private async Task TogglePendingDeleteAsync(ScannedFileDisplayItem? item)
    {
        if (item is null) return;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var file = await db.ScannedFiles.FindAsync(item.Id);
        if (file is not null)
        {
            file.PendingDelete = !file.PendingDelete;
            await db.SaveChangesAsync();
        }

        await LoadPendingFilesAsync();
    }

    private async Task DeleteAllAsync()
    {
        if (PendingDeleteFiles.Count == 0)
            return;

        IsDeleting = true;
        StatusMessage = "Deleting files...";

        try
        {
            var filePaths = PendingDeleteFiles.Select(f => f.FullPath).ToList();

            await _fileDeleteService.DeleteFilesAsync(filePaths, moveToRecycleBin: true);

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var ids = PendingDeleteFiles.Select(f => f.Id).ToList();
            var filesToRemove = await db.ScannedFiles.Where(f => ids.Contains(f.Id)).ToListAsync();
            foreach (var file in filesToRemove) db.ScannedFiles.Remove(file);
            await db.SaveChangesAsync();

            StatusMessage = $"Successfully deleted {filePaths.Count} file(s) to recycle bin.";
            await LoadPendingFilesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting files: {ex.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    private async Task ClearMarkingsAsync()
    {
        if (PendingDeleteFiles.Count == 0)
            return;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var ids = PendingDeleteFiles.Select(f => f.Id).ToList();
        var files = await db.ScannedFiles.Where(f => ids.Contains(f.Id)).ToListAsync();
        foreach (var file in files) file.PendingDelete = false;
        await db.SaveChangesAsync();

        StatusMessage = "All delete markings cleared.";
        await LoadPendingFilesAsync();
    }

    private async Task ViewFileAsync(ScannedFileDisplayItem? item) => await _fileViewerService.ViewFileAsync(item);

    private async Task LoadPendingFilesAsync()
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var files = await db.ScannedFiles
                .Where(f => f.PendingDelete)
                .OrderBy(f => f.FolderPath)
                .ThenBy(f => f.FileName)
                .ToListAsync();

            PendingDeleteFiles.Clear();
            files.ForEach(file => PendingDeleteFiles.Add(new ScannedFileDisplayItem(file)));

            PendingDeleteCount = PendingDeleteFiles.Count;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading pending files: {ex.Message}";
        }
    }
}

using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace AStar.Dev.File.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const string SelectedFolderPathKey = "SelectedFolderPath";

    private readonly IFileScannerService _fileScannerService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileViewerService _fileViewerService;
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private bool _suppressPageReload;

    public static IReadOnlyList<int> PageSizes { get; } = [25, 50, 75, 100, 125, 150, 175, 200];

    [Reactive] public string SelectedFolderPath { get; set; } = string.Empty;
    [Reactive] public bool IsScanning { get; set; }
    [Reactive] public string CurrentScanFolder { get; set; } = string.Empty;
    [Reactive] public int TotalFilesProcessed { get; set; }
    [Reactive] public int PageSize { get; set; } = 50;
    [Reactive] public int CurrentPage { get; set; } = 1;
    [Reactive] public int TotalFileCount { get; set; }
    [Reactive] public bool ShowDuplicatesOnly { get; set; }

    private readonly ObservableAsPropertyHelper<int> _totalPages;
    public int TotalPages => _totalPages.Value;

    private readonly ObservableAsPropertyHelper<string> _pagingInfo;
    public string PagingInfo => _pagingInfo.Value;

    public ObservableCollection<string> StatusMessages { get; } = [];
    public ObservableCollection<ScannedFileDisplayItem> ScannedFiles { get; } = [];

    public event Action<ScannedFileDisplayItem>? ViewFileRequested;
    public event Action? OpenDeleteWindowRequested;

    public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> StartScanCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadFromDatabaseCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenDeleteWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleDuplicatesOnlyCommand { get; }
    public ReactiveCommand<ScannedFileDisplayItem?, Unit> TogglePendingDeleteCommand { get; }
    public ReactiveCommand<ScannedFileDisplayItem?, Unit> ViewFileCommand { get; }
    public ReactiveCommand<Unit, Unit> FirstPageCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
    public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
    public ReactiveCommand<Unit, Unit> LastPageCommand { get; }

    public MainWindowViewModel(IFileScannerService fileScannerService, IFolderPickerService folderPickerService, IFileViewerService fileViewerService, IDbContextFactory<FileAppDbContext> dbContextFactory)
    {
        _fileScannerService = fileScannerService;
        _folderPickerService = folderPickerService;
        _fileViewerService = fileViewerService;
        _dbContextFactory = dbContextFactory;

        _totalPages = this.WhenAnyValue(x => x.TotalFileCount, x => x.PageSize,
            (count, size) => count == 0 ? 1 : (int)Math.Ceiling((double)count / size))
            .ToProperty(this, x => x.TotalPages, initialValue: 1);

        _pagingInfo = this.WhenAnyValue(x => x.CurrentPage, x => x.TotalFileCount, x => x.PageSize,
            (page, count, size) =>
            {
                int total = count == 0 ? 1 : (int)Math.Ceiling((double)count / size);

                return $"PAGE {page} OF {total}  [{count} FILES]";
            })
            .ToProperty(this, x => x.PagingInfo, initialValue: "PAGE 1 OF 1  [0 FILES]");

        var canSelectFolder = this.WhenAnyValue(x => x.IsScanning, scanning => !scanning);
        SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderAsync, canSelectFolder);

        var canScan = this.WhenAnyValue(x => x.IsScanning, x => x.SelectedFolderPath, (scanning, path) => !scanning && !string.IsNullOrWhiteSpace(path));
        StartScanCommand = ReactiveCommand.CreateFromTask(StartScanAsync, canScan);

        var canCancel = this.WhenAnyValue(x => x.IsScanning);
        CancelCommand = ReactiveCommand.Create(Cancel, canCancel);

        var canLoad = this.WhenAnyValue(x => x.SelectedFolderPath, path => !string.IsNullOrWhiteSpace(path));
        LoadFromDatabaseCommand = ReactiveCommand.CreateFromTask(LoadFromDatabaseAsync, canLoad);

        OpenDeleteWindowCommand = ReactiveCommand.Create(() => OpenDeleteWindowRequested?.Invoke());
        ToggleDuplicatesOnlyCommand = ReactiveCommand.Create(() => { ShowDuplicatesOnly = !ShowDuplicatesOnly; });

        TogglePendingDeleteCommand = ReactiveCommand.CreateFromTask<ScannedFileDisplayItem?>(TogglePendingDeleteAsync);
        ViewFileCommand = ReactiveCommand.CreateFromTask<ScannedFileDisplayItem?>(ViewFileAsync);

        var canGoToPreviousPage = this.WhenAnyValue(x => x.CurrentPage, page => page > 1);
        FirstPageCommand = ReactiveCommand.Create(() => { CurrentPage = 1; }, canGoToPreviousPage);
        PreviousPageCommand = ReactiveCommand.Create(() => { CurrentPage--; }, canGoToPreviousPage);

        var canGoToNextPage = this.WhenAnyValue(x => x.CurrentPage, x => x.TotalPages, (page, total) => page < total);
        NextPageCommand = ReactiveCommand.Create(() => { CurrentPage++; }, canGoToNextPage);
        LastPageCommand = ReactiveCommand.Create(() => { CurrentPage = TotalPages; }, canGoToNextPage);

        this.WhenAnyValue(x => x.CurrentPage)
            .Skip(1)
            .Subscribe(__ => { if (!_suppressPageReload) _ = LoadScannedFilesAsync(); })
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.PageSize)
            .Skip(1)
            .Subscribe(__ => { SetPageWithoutReload(1); _ = LoadScannedFilesAsync(); })
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.ShowDuplicatesOnly)
            .Skip(1)
            .Subscribe(__ => { SetPageWithoutReload(1); _ = LoadScannedFilesAsync(); })
            .DisposeWith(Disposables);

        void onFileViewRequested(ScannedFileDisplayItem item) => ViewFileRequested?.Invoke(item);
        _fileViewerService.FileViewRequested += onFileViewRequested;
        Disposable.Create(() => _fileViewerService.FileViewRequested -= onFileViewRequested).DisposeWith(Disposables);

        _ = LoadSelectedFolderPathAsync();
    }

    private async Task LoadSelectedFolderPathAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == SelectedFolderPathKey);

        if (setting is not null && !string.IsNullOrWhiteSpace(setting.Value) && Directory.Exists(setting.Value))
            SelectedFolderPath = setting.Value;
        else
            SelectedFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private async Task SelectFolderAsync()
    {
        string? path = await _folderPickerService.OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(path))
        {
            SelectedFolderPath = path;
            await SaveSelectedFolderPathAsync(path);
        }
    }

    private async Task SaveSelectedFolderPathAsync(string path)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == SelectedFolderPathKey);

        if (setting is not null)
            setting.Value = path;
        else
            db.AppSettings.Add(new AppSetting { Key = SelectedFolderPathKey, Value = path });

        await db.SaveChangesAsync();
    }

    private async Task LoadFromDatabaseAsync()
    {
        SetPageWithoutReload(1);
        await LoadScannedFilesAsync();
    }

    private async Task StartScanAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderPath) || IsScanning)
            return;

        IsScanning = true;
        StatusMessages.Clear();
        ScannedFiles.Clear();
        TotalFilesProcessed = 0;
        TotalFileCount = 0;
        CurrentScanFolder = string.Empty;
        SetPageWithoutReload(1);

        CancellationTokenSource = new CancellationTokenSource();

        var progress = new Progress<ScanProgressUpdate>(update =>
        {
            CurrentScanFolder = update.CurrentFolder;
            TotalFilesProcessed = update.TotalFilesProcessed;
            if (!string.IsNullOrEmpty(update.StatusMessage))
                StatusMessages.Add(update.StatusMessage);
        });

        try
        {
            await Task.Run(() => _fileScannerService.ScanAsync(SelectedFolderPath, progress, CancellationTokenSource.Token), CancellationTokenSource.Token);
            await LoadScannedFilesAsync();
        }
        catch (OperationCanceledException)
        {
            string time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            StatusMessages.Add($"[{time}] [CANCELLED] Scan cancelled by user.");
        }
        finally
        {
            IsScanning = false;
            CancellationTokenSource.Dispose();
        }
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
            item.PendingDelete = file.PendingDelete;
        }
    }

    private async Task ViewFileAsync(ScannedFileDisplayItem? item) => await _fileViewerService.ViewFileAsync(item);

    private void Cancel() => CancellationTokenSource.Cancel();

    private async Task LoadScannedFilesAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedFolderPath)) return;

            string prefix = SelectedFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var baseQuery = db.ScannedFiles.Where(f => f.FullPath.StartsWith(prefix));

            IQueryable<ScannedFile> query;
            if (ShowDuplicatesOnly)
            {
                var duplicateSizeSubquery = baseQuery
                    .GroupBy(f => f.SizeInBytes)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                query = baseQuery
                    .Where(f => duplicateSizeSubquery.Contains(f.SizeInBytes))
                    .OrderBy(f => f.SizeInBytes)
                    .ThenBy(f => f.FolderPath)
                    .ThenBy(f => f.FileName);
            }
            else
            {
                query = baseQuery
                    .OrderBy(f => f.FolderPath)
                    .ThenBy(f => f.FileName);
            }

            TotalFileCount = await query.CountAsync();

            ClampCurrentPage();

            var files = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ScannedFiles.Clear();
            files.ForEach(file => ScannedFiles.Add(new ScannedFileDisplayItem(file)));
        }
        catch (Exception ex)
        {
            StatusMessages.Add($"Error loading files: {ex.Message}");
        }
    }

    private void ClampCurrentPage()
    {
        if (CurrentPage <= TotalPages) return;

        SetPageWithoutReload(TotalPages);
    }

    private void SetPageWithoutReload(int page)
    {
        _suppressPageReload = true;
        CurrentPage = page;
        _suppressPageReload = false;
    }
}

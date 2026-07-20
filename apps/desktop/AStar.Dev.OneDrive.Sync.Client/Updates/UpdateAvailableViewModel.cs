using AStar.Dev.Logging.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <summary>Presents a discovered Velopack update, offering the user a choice to restart now or later.</summary>
public sealed partial class UpdateAvailableViewModel : ObservableObject
{
    private readonly UpdateInfo updateInfo;
    private readonly IUpdateCheckService updateCheckService;
    private readonly ILogger<UpdateAvailableViewModel> logger;

    public UpdateAvailableViewModel(UpdateInfo updateInfo, IUpdateCheckService updateCheckService, ILogger<UpdateAvailableViewModel> logger)
    {
        this.updateInfo = updateInfo;
        this.updateCheckService = updateCheckService;
        this.logger = logger;
        TargetVersion = updateInfo.TargetFullRelease.Version.ToString();
        ReleaseNotes = updateInfo.TargetFullRelease.NotesMarkdown ?? string.Empty;
    }

    [ObservableProperty]
    public partial string TargetVersion { get; set; }

    [ObservableProperty]
    public partial string ReleaseNotes { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Raised once the dialog should close, whether the user restarted or deferred.</summary>
    public event EventHandler? CloseRequested;

    [RelayCommand]
    private async Task RestartNowAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await updateCheckService.DownloadUpdatesAsync(updateInfo);
            updateCheckService.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (Exception ex)
        {
            LogMessage.LogException(logger, nameof(UpdateAvailableViewModel), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Later() => CloseRequested?.Invoke(this, EventArgs.Empty);
}

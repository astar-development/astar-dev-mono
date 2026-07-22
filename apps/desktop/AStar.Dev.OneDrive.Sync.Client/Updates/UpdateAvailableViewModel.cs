using System.Globalization;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.Velopack.Publishing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <summary>Presents a discovered Velopack update, offering the user a choice to restart now or later.</summary>
public sealed partial class UpdateAvailableViewModel : ObservableObject, IDisposable
{
    private readonly UpdateInfo updateInfo;
    private readonly IVelopackUpdateService updateCheckService;
    private readonly ILocalizationService loc;
    private readonly ILogger<UpdateAvailableViewModel> logger;

    public UpdateAvailableViewModel(UpdateInfo updateInfo, IVelopackUpdateService updateCheckService, ILocalizationService localizationService, ILogger<UpdateAvailableViewModel> logger)
    {
        this.updateInfo = updateInfo;
        this.updateCheckService = updateCheckService;
        loc = localizationService;
        this.logger = logger;
        loc.CultureChanged += OnCultureChanged;
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

    public string Title => loc.GetLocal("Update.Title");
    public string Message => loc.GetLocal("Update.Message", TargetVersion);
    public string ReleaseNotesLabel => loc.GetLocal("Update.ReleaseNotesLabel");
    public string RestartNowLabel => loc.GetLocal("Update.RestartNow");
    public string LaterLabel => loc.GetLocal("Update.Later");
    public string DownloadingLabel => loc.GetLocal("Update.Downloading");

    /// <summary>Raised once the dialog should close, whether the user restarted or deferred.</summary>
    public event EventHandler? CloseRequested;

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(ReleaseNotesLabel));
        OnPropertyChanged(nameof(RestartNowLabel));
        OnPropertyChanged(nameof(LaterLabel));
        OnPropertyChanged(nameof(DownloadingLabel));
    }

    public void Dispose() => loc.CultureChanged -= OnCultureChanged;

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

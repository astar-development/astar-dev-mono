using AStar.Dev.Logging.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Presents a discovered Velopack update, offering the user a choice to restart now or later.</summary>
public sealed partial class UpdateAvailableViewModel : ObservableObject, IDisposable
{
    private readonly UpdateInfo updateInfo;
    private readonly IVelopackUpdateService updateCheckService;
    private readonly IUpdateDialogTextProvider textProvider;
    private readonly ILogger<UpdateAvailableViewModel> logger;

    /// <summary>Creates the view model for the supplied discovered update.</summary>
    /// <param name="updateInfo">The discovered update, as returned by <see cref="IVelopackUpdateService.CheckForUpdatesAsync"/>.</param>
    /// <param name="updateCheckService">The service used to download and apply the update.</param>
    /// <param name="textProvider">Supplies the dialog's display text.</param>
    /// <param name="logger">The logger used to record download/apply failures.</param>
    public UpdateAvailableViewModel(UpdateInfo updateInfo, IVelopackUpdateService updateCheckService, IUpdateDialogTextProvider textProvider, ILogger<UpdateAvailableViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);
        ArgumentNullException.ThrowIfNull(updateCheckService);
        ArgumentNullException.ThrowIfNull(textProvider);
        ArgumentNullException.ThrowIfNull(logger);

        this.updateInfo = updateInfo;
        this.updateCheckService = updateCheckService;
        this.textProvider = textProvider;
        this.logger = logger;
        this.textProvider.TextChanged += OnTextProviderChanged;
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

    /// <summary>The dialog's title text.</summary>
    public string Title => textProvider.Title;

    /// <summary>The body message describing the available update.</summary>
    public string Message => textProvider.GetMessage(TargetVersion);

    /// <summary>The label shown above the release notes section.</summary>
    public string ReleaseNotesLabel => textProvider.ReleaseNotesLabel;

    /// <summary>The label for the button that restarts the app to apply the update immediately.</summary>
    public string RestartNowLabel => textProvider.RestartNowLabel;

    /// <summary>The label for the button that dismisses the dialog without updating.</summary>
    public string LaterLabel => textProvider.LaterLabel;

    /// <summary>The label shown while the update is downloading.</summary>
    public string DownloadingLabel => textProvider.DownloadingLabel;

    /// <summary>Raised once the dialog should close, whether the user restarted or deferred.</summary>
    public event EventHandler? CloseRequested;

    private void OnTextProviderChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(ReleaseNotesLabel));
        OnPropertyChanged(nameof(RestartNowLabel));
        OnPropertyChanged(nameof(LaterLabel));
        OnPropertyChanged(nameof(DownloadingLabel));
    }

    /// <summary>Unsubscribes from the <see cref="IUpdateDialogTextProvider.TextChanged"/> event.</summary>
    public void Dispose() => textProvider.TextChanged -= OnTextProviderChanged;

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

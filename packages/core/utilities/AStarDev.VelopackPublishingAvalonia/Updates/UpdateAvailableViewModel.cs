using System.Globalization;
using AStarDev.LoggingExtensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Presents a discovered Velopack update, offering the user a choice to restart now or later.</summary>
public sealed partial class UpdateAvailableViewModel : ObservableObject, IDisposable
{
    private const double BytesPerMegabyte = 1024 * 1024;

    private readonly UpdateInfo updateInfo;
    private readonly IVelopackUpdateService updateCheckService;
    private readonly IUpdateApplyGate applyGate;
    private readonly IUpdateDialogTextProvider textProvider;
    private readonly UpdateAction action;
    private readonly ILogger<UpdateAvailableViewModel> logger;
    private bool disposed;

    /// <summary>Creates the view model for the supplied discovered update.</summary>
    /// <param name="updateInfo">The discovered update, as returned by <see cref="IVelopackUpdateService.CheckForUpdatesAsync"/>.</param>
    /// <param name="updateCheckService">The service used to download and apply the update.</param>
    /// <param name="applyGate">Holds the update back until the app can restart.</param>
    /// <param name="textProvider">Supplies the dialog's display text.</param>
    /// <param name="logger">The logger used to record download/apply failures.</param>
    public UpdateAvailableViewModel(UpdateInfo updateInfo, IVelopackUpdateService updateCheckService, IUpdateApplyGate applyGate, IUpdateDialogTextProvider textProvider, ILogger<UpdateAvailableViewModel> logger)
        : this(updateInfo, updateCheckService, applyGate, textProvider, logger, UpdateActionFactory.CreatePrompt())
    {
    }

    /// <summary>Creates the view model for the supplied discovered update, for the given reason it is being shown.</summary>
    /// <param name="updateInfo">The discovered update, as returned by <see cref="IVelopackUpdateService.CheckForUpdatesAsync"/>.</param>
    /// <param name="updateCheckService">The service used to download and apply the update.</param>
    /// <param name="applyGate">Holds the update back until the app can restart.</param>
    /// <param name="textProvider">Supplies the dialog's display text.</param>
    /// <param name="logger">The logger used to record download/apply failures.</param>
    /// <param name="action">Why the dialog is shown: <see cref="ForceAfterWarning"/> warns that the update is about to be installed and cannot be deferred; anything else is an ordinary prompt.</param>
    public UpdateAvailableViewModel(UpdateInfo updateInfo, IVelopackUpdateService updateCheckService, IUpdateApplyGate applyGate, IUpdateDialogTextProvider textProvider, ILogger<UpdateAvailableViewModel> logger, UpdateAction action)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);
        ArgumentNullException.ThrowIfNull(updateCheckService);
        ArgumentNullException.ThrowIfNull(applyGate);
        ArgumentNullException.ThrowIfNull(textProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(action);

        this.action = action;
        this.updateInfo = updateInfo;
        this.updateCheckService = updateCheckService;
        this.applyGate = applyGate;
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

    [ObservableProperty]
    public partial int DownloadProgress { get; set; }

    [ObservableProperty]
    public partial string DownloadProgressText { get; set; } = string.Empty;

    /// <summary>The dialog's title text.</summary>
    public string Title => textProvider.Title;

    /// <summary>The body message describing the available update.</summary>
    public string Message => IsForced ? textProvider.GetForcedMessage(TargetVersion) : textProvider.GetMessage(TargetVersion);

    /// <summary>Whether the update has been deferred for too long and is about to be installed.</summary>
    public bool IsForced => action is ForceAfterWarning;

    /// <summary>Whether the user may install later. A forced update cannot be deferred.</summary>
    public bool CanDefer => !IsForced;

    /// <summary>How the user left the dialog. It is <see cref="Dismissed"/> until they choose.</summary>
    public UpdateDialogOutcome Outcome { get; private set; } = UpdateDialogOutcomeFactory.CreateDismissed();

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
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            textProvider.TextChanged -= OnTextProviderChanged;
    }

    [RelayCommand]
    private async Task RestartNowAsync()
    {
        Outcome = UpdateDialogOutcomeFactory.CreateInstall();
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            DownloadProgress = 0;
            DownloadProgressText = string.Empty;
            var uiContext = SynchronizationContext.Current;
            await updateCheckService.DownloadUpdatesAsync(updateInfo, percent => PostToUi(uiContext, () => ReportProgress(percent)));
            await ApplyWhenSafeAsync();
        }
        catch (Exception ex)
        {
            LogMessage.ExceptionCaught(logger, nameof(UpdateAvailableViewModel), ex.ErrorType());
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyWhenSafeAsync()
    {
        var permit = await applyGate.WaitUntilSafeAsync(CancellationToken.None);
        var applied = false;

        try
        {
            updateCheckService.ApplyUpdatesAndRestart(updateInfo);
            applied = true;
        }
        finally
        {
            if (!applied)
                permit.Dispose();
        }
    }

    private static void PostToUi(SynchronizationContext? uiContext, Action action)
    {
        if (uiContext is null)
        {
            action();

            return;
        }

        uiContext.Post(_ => action(), null);
    }

    private void ReportProgress(int percent)
    {
        int clamped = Math.Clamp(percent, 0, 100);
        double totalMegabytes = DownloadSizeInBytes() / BytesPerMegabyte;
        double downloadedMegabytes = totalMegabytes * clamped / 100;

        DownloadProgress = clamped;
        DownloadProgressText = string.Create(CultureInfo.InvariantCulture, $"{clamped}% · {downloadedMegabytes:F1} / {totalMegabytes:F1} MB");
    }

    private double DownloadSizeInBytes()
        => updateInfo.DeltasToTarget.Length > 0 ? updateInfo.DeltasToTarget.Sum(delta => delta.Size) : updateInfo.TargetFullRelease.Size;

    [RelayCommand]
    private void Later()
    {
        Outcome = UpdateDialogOutcomeFactory.CreateDefer();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

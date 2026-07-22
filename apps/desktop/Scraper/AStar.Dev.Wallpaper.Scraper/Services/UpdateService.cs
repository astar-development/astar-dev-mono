using AStar.Dev.Logging.Extensions;
using AStar.Dev.Velopack.Publishing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <summary>
///     Checks GitHub Releases for a newer Velopack package, asks the user whether to download and
///     restart, and - only if they agree - downloads it and applies the update.
/// </summary>
public sealed class UpdateService(IVelopackUpdateService updateService, ILogger<UpdateService> logger)
{
    /// <summary>
    ///     Checks for updates, and - if one is available and the user agrees - downloads and applies it.
    /// </summary>
    /// <param name="owner">The owner window for the update prompt dialog.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckForUpdatesAsync(Window owner)
    {
        try
        {
            var updateInfo = await updateService.CheckForUpdatesAsync();
            if (updateInfo is null)
                return;

            string version = updateInfo.TargetFullRelease.Version.ToString();
            bool shouldDownload = await Dispatcher.UIThread.InvokeAsync(() => PromptToDownloadAsync(owner, version));

            if (!shouldDownload)
            {
                string declinedMessage = $"Update {version} declined by user.";
                LogMessage.Information(logger, nameof(UpdateService), declinedMessage);

                return;
            }

            await updateService.DownloadUpdatesAsync(updateInfo, new DownloadProgressReporter(logger).Report);
            string appliedMessage = $"Update {version} downloaded; applying and restarting.";
            LogMessage.Information(logger, nameof(UpdateService), appliedMessage);
            updateService.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (Exception ex)
        {
            LogMessage.LogException(logger, nameof(UpdateService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    private static async Task<bool> PromptToDownloadAsync(Window owner, string version)
    {
        bool shouldDownload = false;

        var dialog = new Window
        {
            Title = "Update available",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var downloadButton = new Button { Content = "Download and restart" };
        var laterButton = new Button { Content = "Later" };

        downloadButton.Click += (_, _) =>
        {
            shouldDownload = true;
            dialog.Close();
        };

        laterButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = $"Version {version} is available. Download and restart to update?" },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { laterButton, downloadButton },
                },
            },
        };

        await dialog.ShowDialog(owner);

        return shouldDownload;
    }

    private sealed class DownloadProgressReporter(ILogger<UpdateService> logger)
    {
        private int lastLoggedProgress;

        public void Report(int progress)
        {
            if (progress < lastLoggedProgress + 25)
                return;

            lastLoggedProgress = progress;
            string progressMessage = $"Download progress {progress}%";
            LogMessage.Information(logger, nameof(UpdateService), progressMessage);
        }
    }
}

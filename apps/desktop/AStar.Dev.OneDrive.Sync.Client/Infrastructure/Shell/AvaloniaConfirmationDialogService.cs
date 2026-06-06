using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AvaloniaConfirmationDialogService : IConfirmationDialogService
{
    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, CancellationToken ct = default)
        => await Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(title, message, ct));

    private static async Task<bool> ShowDialogAsync(string title, string message, CancellationToken ct)
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow is null)
            return false;

        var tcs = new TaskCompletionSource<bool>();

        var messageBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var cancelButton = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0) };
        var yesButton = new Button { Content = "Yes" };

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, yesButton }
        };

        var content = new StackPanel
        {
            Margin = new Thickness(24),
            Children = { messageBlock, buttonRow }
        };

        var dialog = new Window
        {
            Title = title,
            Content = content,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        cancelButton.Click += (_, _) => { tcs.SetResult(false); dialog.Close(); };
        yesButton.Click += (_, _) => { tcs.SetResult(true); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        ct.Register(() => { tcs.TrySetResult(false); dialog.Close(); });

        await dialog.ShowDialog(mainWindow).ConfigureAwait(false);

        return await tcs.Task.ConfigureAwait(false);
    }
}

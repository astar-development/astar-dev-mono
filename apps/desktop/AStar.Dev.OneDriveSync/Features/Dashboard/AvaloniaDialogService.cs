using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>
///     Avalonia-backed implementation of <see cref="IDialogService"/> (S012).
///     Uses <see cref="Window.ShowDialog{TResult}"/> so dialogs are properly modal
///     — no <c>MessageBox.Show()</c> (implementation constraint, S012).
/// </summary>
internal sealed class AvaloniaDialogService : IDialogService
{
    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, CancellationToken ct = default)
    {
        var owner = GetMainWindow();
        if (owner is null)

            return false;

        var dialog = new ConfirmationDialog { Title = title, Message = message };

        return await dialog.ShowDialog<bool>(owner).ConfigureAwait(true);
    }

    private static Window? GetMainWindow()
        => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}

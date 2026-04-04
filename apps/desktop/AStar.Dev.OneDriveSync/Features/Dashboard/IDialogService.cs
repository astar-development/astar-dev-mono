using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>Shows modal confirmation dialogs from the Dashboard feature (S012).</summary>
public interface IDialogService
{
    /// <summary>
    ///     Presents a Yes/No dialog with the given <paramref name="title"/> and <paramref name="message"/>.
    ///     Returns <see langword="true"/> when the user confirms.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, CancellationToken ct = default);
}

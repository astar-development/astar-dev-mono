namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <summary>Abstracts platform-level confirmation dialogs to keep consumers testable without Avalonia infrastructure.</summary>
public interface IConfirmationDialogService
{
    /// <summary>Shows a yes/no confirmation dialog. Returns <c>true</c> if the user confirmed; <c>false</c> if cancelled or denied.</summary>
    Task<bool> ConfirmAsync(string title, string message, CancellationToken ct = default);
}

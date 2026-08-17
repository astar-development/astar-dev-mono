namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Abstracts showing the update-available modal dialog to keep consumers testable without Avalonia infrastructure.</summary>
public interface IUpdateAvailableDialogService
{
    /// <summary>Shows the update-available dialog bound to <paramref name="viewModel"/> and waits until it closes.</summary>
    Task ShowAsync(UpdateAvailableViewModel viewModel, CancellationToken cancellationToken = default);
}

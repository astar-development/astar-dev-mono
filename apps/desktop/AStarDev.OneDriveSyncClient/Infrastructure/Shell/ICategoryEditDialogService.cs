using AStarDev.OneDriveSyncClient.Classifications;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <summary>Abstracts the modal category-edit dialog to keep consumers testable without Avalonia infrastructure.</summary>
public interface ICategoryEditDialogService
{
    /// <summary>Shows the edit dialog for the given category node. Returns once the dialog closes (either via Save or Cancel).</summary>
    Task ShowAsync(CategoryNodeViewModel node, CancellationToken cancellationToken = default);
}

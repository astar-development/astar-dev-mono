using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Keeps how long the <c>.sync-trash</c> folder holds a removed item (#1593).</summary>
public interface ITrashRetentionSettings
{
    /// <summary>Reads the retention.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The saved retention, the default when none was saved or the saved value is invalid, or the error that stopped the read.</returns>
    Task<Result<LocalTrashOptions, ErrorResponse>> GetAsync(CancellationToken cancellationToken);

    /// <summary>Saves the retention. The next download pass uses it.</summary>
    /// <param name="options">The retention to save.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The saved retention, or the error that stopped the save.</returns>
    Task<Result<LocalTrashOptions, ErrorResponse>> SetAsync(LocalTrashOptions options, CancellationToken cancellationToken);
}

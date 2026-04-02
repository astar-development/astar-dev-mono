using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     Validates local sync path selection for account configuration (AM-06, AM-07).
/// </summary>
public interface ILocalSyncPathService
{
    /// <summary>
    ///     Returns <c>Success</c> if <paramref name="candidatePath"/> does not overlap any existing
    ///     account's sync path (excluding <paramref name="excludeAccountId"/> for edit scenarios).
    ///     Returns <c>Failure</c> with a user-visible message if an overlap is detected (AM-07).
    /// </summary>
    Task<Result<bool, string>> ValidateNoOverlapAsync(string candidatePath, Guid? excludeAccountId = null, CancellationToken ct = default);

    /// <summary>
    ///     Returns <c>true</c> if the folder at <paramref name="path"/> exists and contains files (AM-10).
    /// </summary>
    bool IsNonEmpty(string path);

    /// <summary>
    ///     Returns the default local sync path for an account: <c>~/OneDrive/&lt;displayName&gt;/</c> (AM-07).
    /// </summary>
    string GetDefaultPath(string accountDisplayName);
}

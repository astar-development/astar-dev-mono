using global::Velopack;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>Creates <see cref="UpdateCheckResult"/> cases.</summary>
public static class UpdateCheckResultFactory
{
    private const string UnknownReason = "Unknown error.";

    /// <summary>Creates a result for a found update. A missing <paramref name="updateInfo"/> yields <see cref="CheckFailed"/>.</summary>
    /// <param name="updateInfo">The update that was found.</param>
    public static UpdateCheckResult CreateFound(UpdateInfo updateInfo) => updateInfo is null ? CreateFailed("Update information was missing.") : new UpdateFound(updateInfo);

    /// <summary>Creates the up-to-date result.</summary>
    public static UpdateCheckResult CreateUpToDate() => new UpToDate();

    /// <summary>Creates the not-installed result.</summary>
    public static UpdateCheckResult CreateNotInstalled() => new NotInstalled();

    /// <summary>Creates a failed result. A blank <paramref name="reason"/> is replaced with a default.</summary>
    /// <param name="reason">Why the check failed.</param>
    public static UpdateCheckResult CreateFailed(string? reason) => new CheckFailed(string.IsNullOrWhiteSpace(reason) ? UnknownReason : reason);
}

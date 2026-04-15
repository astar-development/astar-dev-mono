namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>
/// Immutable value object representing a validated local sync path.
/// Construct via <see cref="LocalSyncPathFactory.Create"/> — never use the EF restore path directly.
/// </summary>
public sealed record LocalSyncPath
{
    private LocalSyncPath(string value) => Value = value;

    /// <summary>The underlying path string.</summary>
    public string Value { get; }

    /// <summary>
    /// Restores a <see cref="LocalSyncPath"/> from a persisted value.
    /// Bypasses validation — for EF Core value converter use only.
    /// </summary>
    internal static LocalSyncPath Restore(string value) => new(value);
}

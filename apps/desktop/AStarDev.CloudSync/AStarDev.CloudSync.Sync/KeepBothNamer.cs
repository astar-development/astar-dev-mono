namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class KeepBothNamer : IKeepBothNamer
{
    /// <inheritdoc />
    public string Name(string original, DateTimeOffset utc, IReadOnlyCollection<string> existingNames) =>
        throw new NotImplementedException($"{nameof(Name)} {original} {utc} {existingNames}");
}

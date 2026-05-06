namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <inheritdoc />
public sealed record ItemPath(string Name, string? RelativePath = null)
{
    /// <summary>Returns <see cref="RelativePath"/> when set, otherwise <see cref="Name"/>.</summary>
    public string EffectivePath => RelativePath ?? Name;
}

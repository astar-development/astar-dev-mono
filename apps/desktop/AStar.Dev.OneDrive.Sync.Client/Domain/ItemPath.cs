using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <inheritdoc />
public sealed record ItemPath(string Name, Option<string> RelativePath)
{
    /// <summary>Returns <see cref="RelativePath"/> when set, otherwise <see cref="Name"/>.</summary>
    public string EffectivePath => RelativePath.Match(p => p, () => Name);
}

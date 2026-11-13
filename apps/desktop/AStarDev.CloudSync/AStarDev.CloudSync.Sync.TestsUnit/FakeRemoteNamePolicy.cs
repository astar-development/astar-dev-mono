using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeRemoteNamePolicy : IRemoteNamePolicy
{
    private readonly List<(string Name, long Size)> checks = [];

    public HashSet<string> Rejected { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<(string Name, long Size)> Checks => checks;

    public Result<Unit, InvalidRemoteName> Check(string name, RelativeSyncPath path, long size)
    {
        checks.Add((name, size));

        return Rejected.Contains(name)
            ? Result.Failure<Unit, InvalidRemoteName>(InvalidRemoteNameFactory.CreateReservedName())
            : Result.Success<Unit, InvalidRemoteName>(Unit.Instance);
    }
}

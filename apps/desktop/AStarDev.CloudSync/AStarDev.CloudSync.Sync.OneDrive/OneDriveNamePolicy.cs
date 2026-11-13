using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>The names, path lengths and file sizes OneDrive accepts.</summary>
internal sealed class OneDriveNamePolicy : IRemoteNamePolicy
{
    /// <inheritdoc />
    public Result<Unit, InvalidRemoteName> Check(string name, RelativeSyncPath path, long size) => throw new NotImplementedException($"{name}{path}{size}");
}

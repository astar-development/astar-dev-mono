using Testably.Abstractions.Helpers;
using Testably.Abstractions.Testing.FileSystem;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class DenyListing : IAccessControlStrategy
{
    private volatile string denied = string.Empty;
    private volatile bool armed;

    public void Target(string fullPath, bool armNow)
    {
        denied = Normalise(fullPath);
        armed = armNow;
    }

    public void ArmWhenSeen(string fullPath)
    {
        if (string.Equals(Normalise(fullPath), denied, StringComparison.OrdinalIgnoreCase))
            armed = true;
    }

    public bool IsAccessGranted(string fullPath, IFileSystemExtensibility extensibility) => !armed || !string.Equals(Normalise(fullPath), denied, StringComparison.OrdinalIgnoreCase);

    private static string Normalise(string path) => path.Replace('/', '\\').TrimEnd('\\');
}

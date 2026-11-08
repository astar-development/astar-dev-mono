using System.IO.Abstractions;
using Testably.Abstractions;

namespace AStarDev.SingleInstance.TestsUnit;

internal sealed class TemporaryDataFolder : IDisposable
{
    private readonly IDirectoryInfo root;

    public TemporaryDataFolder(string? leafName = null)
    {
        var fileSystem = new RealFileSystem();
        root = fileSystem.DirectoryInfo.New(fileSystem.Path.Join(fileSystem.Path.GetTempPath(), $"si-{Guid.NewGuid():N}"[..11]));
        Folder = fileSystem.DirectoryInfo.New(fileSystem.Path.Join(root.FullName, leafName ?? "data"));
    }

    public IDirectoryInfo Folder { get; }

    public string LockPath => Folder.FileSystem.Path.Join(Folder.FullName, "app.lock");

    public string SocketPath => Folder.FileSystem.Path.Join(Folder.FullName, "app.sock");

    public SingleInstanceOptions Options(TimeSpan? handOffTimeout = null) => SingleInstanceOptionsFactory.Create(Folder, "app", handOffTimeout ?? TimeSpan.FromMilliseconds(500));

    public void Dispose()
    {
        if (root.Exists)
            root.Delete(recursive: true);
    }
}

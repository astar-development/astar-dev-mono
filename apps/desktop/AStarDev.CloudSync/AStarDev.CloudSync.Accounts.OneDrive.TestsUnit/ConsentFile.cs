using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class ConsentFile
{
    public const string FolderPath = "/home/user/.config/AStarDev.CloudSync";

    public const string Path = FolderPath + "/token-cache-consent.json";

    public const string GrantedJson = """{"PlaintextCache":"Granted"}""";

    public MockFileSystem FileSystem { get; } = new();

    public IDirectoryInfo Folder => FileSystem.DirectoryInfo.New(FolderPath);

    public IFileInfo File => FileSystem.FileInfo.New(Path);

    public bool Exists => FileSystem.File.Exists(Path);

    public Option<PlaintextCacheConsent> Remembered
    {
        get => Exists && FileSystem.File.ReadAllText(Path).Contains("Granted", StringComparison.Ordinal) ? Option.Some(PlaintextCacheConsentFactory.CreateGranted()) : Option.None<PlaintextCacheConsent>();
        set => value.Match(_ => Write(GrantedJson), Forget);
    }

    public bool Write(string content)
    {
        FileSystem.Directory.CreateDirectory(FolderPath);
        FileSystem.File.WriteAllText(Path, content);

        return true;
    }

    public void RefuseDeletion() => _ = FileSystem.Intercept.Deleting(FileSystemTypes.File, _ => throw new IOException("The consent file is in use."));

    private bool Forget()
    {
        if (Exists)
            FileSystem.File.Delete(Path);

        return false;
    }
}

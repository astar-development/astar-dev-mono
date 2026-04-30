namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("settings-test-").FullName;
    public string SettingsFilePath => System.IO.Path.Combine(Path, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed class EnvironmentSpecialFolderResolver : ISpecialFolderResolver
{
    public string GetLocalApplicationDataPath()
        => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}

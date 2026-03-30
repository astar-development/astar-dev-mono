namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public interface ISpecialFolderResolver
{
    string GetLocalApplicationDataPath();
}

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IFeatureRegistrar
{
    void Register(NavSection section);
    void Freeze();
}

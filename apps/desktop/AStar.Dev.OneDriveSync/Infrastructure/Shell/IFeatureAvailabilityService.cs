namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public interface IFeatureAvailabilityService
{
    bool IsAvailable(NavSection section);
    void Register(NavSection section);
}

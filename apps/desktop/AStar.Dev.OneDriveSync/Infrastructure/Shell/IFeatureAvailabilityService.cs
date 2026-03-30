namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public interface IFeatureAvailabilityService
{
    bool IsAvailable(NavSection section);
}

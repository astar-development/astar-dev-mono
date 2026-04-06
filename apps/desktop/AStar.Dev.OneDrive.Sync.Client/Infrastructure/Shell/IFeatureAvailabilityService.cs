namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IFeatureAvailabilityService
{
    bool IsAvailable(NavSection section);
}

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public sealed class FeatureAvailabilityService : IFeatureAvailabilityService
{
    private readonly HashSet<NavSection> _availableSections = [];

    public bool IsAvailable(NavSection section) => _availableSections.Contains(section);

    public void Register(NavSection section) => _availableSections.Add(section);
}

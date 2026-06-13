using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>Holds the resolved drive ID and root item ID for a specific OneDrive account.</summary>
internal sealed record DriveContext(DriveId DriveId, string RootId);

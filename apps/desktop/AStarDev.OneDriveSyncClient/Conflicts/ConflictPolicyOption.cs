using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Conflicts;

public sealed record ConflictPolicyOption(ConflictPolicy Policy, string Label, string Description, bool IsSelected = false);

using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

public sealed record ConflictPolicyOption(ConflictPolicy Policy, string Label, string Description, bool IsSelected = false);

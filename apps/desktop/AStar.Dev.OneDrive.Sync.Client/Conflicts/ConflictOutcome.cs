namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

public enum ConflictOutcome
{
    Skip,           // Ignore policy — do nothing
    UseLocal,       // Upload local to remote
    UseRemote,      // Download remote to local
    KeepBoth        // Rename local, download remote
}

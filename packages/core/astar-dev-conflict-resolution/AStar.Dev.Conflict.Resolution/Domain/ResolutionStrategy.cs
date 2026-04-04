namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>The strategy chosen by the user to resolve a conflict.</summary>
public enum ResolutionStrategy
{
    /// <summary>The local file overrides the remote version; the remote is discarded.</summary>
    LocalWins,

    /// <summary>The remote file is downloaded; the local version is discarded.</summary>
    RemoteWins,

    /// <summary>Both versions are kept; the conflicting copy is renamed with a UTC timestamp suffix.</summary>
    KeepBoth,

    /// <summary>The conflict is deferred and remains in the queue across sessions.</summary>
    Skip
}

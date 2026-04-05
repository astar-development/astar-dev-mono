namespace AStar.Dev.Sync.Engine.Features.Activity;

/// <summary>Classifies the file operation recorded in the activity feed (S013).</summary>
public enum ActivityActionType
{
    /// <summary>A remote file was downloaded to the local machine.</summary>
    Downloaded,

    /// <summary>A local file was uploaded to OneDrive.</summary>
    Uploaded,

    /// <summary>A file was skipped because it was already in sync.</summary>
    Skipped,

    /// <summary>A conflict between local and remote versions was detected.</summary>
    ConflictDetected,

    /// <summary>A file operation failed.</summary>
    Error
}

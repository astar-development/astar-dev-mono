namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>Describes the nature of a detected sync conflict.</summary>
public enum ConflictType
{
    /// <summary>The file was modified on both the local machine and OneDrive since the last sync.</summary>
    BothModified,

    /// <summary>The file was deleted on one side while the other side still has it.</summary>
    DeletedOnOneSide
}

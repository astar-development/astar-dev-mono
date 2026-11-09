namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>The three states of a folder's checkbox in the selection tree.</summary>
public abstract record FolderCheckState
{
    private FolderCheckState()
    {
    }

    /// <summary>The folder syncs.</summary>
    public sealed record Ticked : FolderCheckState
    {
        internal Ticked()
        {
        }
    }

    /// <summary>The folder does not sync.</summary>
    public sealed record Unticked : FolderCheckState
    {
        internal Unticked()
        {
        }
    }

    /// <summary>The folder's own state differs from a rule below it.</summary>
    public sealed record PartiallyTicked : FolderCheckState
    {
        internal PartiallyTicked()
        {
        }
    }
}

namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Where a folder selection is shown.</summary>
public abstract record FolderSelectionHosting
{
    private FolderSelectionHosting()
    {
    }

    /// <summary>A modal dialog: closes when applied or cancelled.</summary>
    public sealed record Dialog : FolderSelectionHosting
    {
        internal Dialog()
        {
        }
    }

    /// <summary>A page: stays shown after Apply or Cancel and is pointed at an account by the page.</summary>
    public sealed record Page : FolderSelectionHosting
    {
        internal Page()
        {
        }
    }
}

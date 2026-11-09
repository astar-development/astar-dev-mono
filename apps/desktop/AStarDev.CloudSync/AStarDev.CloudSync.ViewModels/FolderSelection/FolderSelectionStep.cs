namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>Where the folder selection dialog is.</summary>
public abstract record FolderSelectionStep
{
    private FolderSelectionStep()
    {
    }

    /// <summary>The dialog is closed.</summary>
    public sealed record Closed : FolderSelectionStep
    {
        internal Closed()
        {
        }
    }

    /// <summary>The user is choosing which account to pick folders for.</summary>
    public sealed record ChoosingAccount : FolderSelectionStep
    {
        internal ChoosingAccount()
        {
        }
    }

    /// <summary>The folder tree of one account is showing.</summary>
    public sealed record Tree : FolderSelectionStep
    {
        internal Tree()
        {
        }
    }

    /// <summary>Asking the user to confirm that already synced folders stop syncing.</summary>
    public sealed record ConfirmingUntick : FolderSelectionStep
    {
        internal ConfirmingUntick(int count)
        {
            Count = count;
        }

        /// <summary>How many already synced folders are being unticked.</summary>
        public int Count { get; }
    }

    /// <summary>The changes are being saved.</summary>
    public sealed record Saving : FolderSelectionStep
    {
        internal Saving()
        {
        }
    }
}

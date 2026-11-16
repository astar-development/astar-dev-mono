namespace AStarDev.CloudSync.Classification;

/// <summary>Which signal produced a candidate.</summary>
public abstract record SignalKind
{
    private SignalKind()
    {
    }

    /// <summary>The file extension.</summary>
    public sealed record Extension : SignalKind
    {
        internal Extension()
        {
        }
    }

    /// <summary>A folder name in the path.</summary>
    public sealed record FolderName : SignalKind
    {
        internal FolderName()
        {
        }
    }

    /// <summary>A word or phrase in the vocabulary.</summary>
    public sealed record Vocabulary : SignalKind
    {
        internal Vocabulary()
        {
        }
    }
}

namespace AStarDev.CloudSync.Classification;

/// <summary>Where a term came from.</summary>
public abstract record TermSource
{
    private TermSource()
    {
    }

    /// <summary>Shipped with the app.</summary>
    public sealed record Seed : TermSource
    {
        internal Seed()
        {
        }
    }

    /// <summary>Added or promoted by the user.</summary>
    public sealed record User : TermSource
    {
        internal User()
        {
        }
    }

    /// <summary>Derived automatically from files.</summary>
    public sealed record Auto : TermSource
    {
        internal Auto()
        {
        }
    }

    /// <summary>Found in file names and waiting for review.</summary>
    public sealed record Suggested : TermSource
    {
        internal Suggested()
        {
        }
    }
}

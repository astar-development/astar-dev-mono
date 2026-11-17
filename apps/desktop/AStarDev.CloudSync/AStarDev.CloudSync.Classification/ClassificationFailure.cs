namespace AStarDev.CloudSync.Classification;

/// <summary>Why a classification store or seed operation failed. Carries no message, so nothing sensitive reaches logs.</summary>
public abstract record ClassificationFailure
{
    private ClassificationFailure()
    {
    }

    /// <summary>The term or category does not exist.</summary>
    public sealed record NotFound : ClassificationFailure
    {
        internal NotFound()
        {
        }
    }

    /// <summary>The file is not in the account's current sync baseline.</summary>
    public sealed record FileNotFound : ClassificationFailure
    {
        internal FileNotFound()
        {
        }
    }

    /// <summary>The store could not be read or written.</summary>
    public sealed record Unavailable : ClassificationFailure
    {
        internal Unavailable()
        {
        }
    }

    /// <summary>The seed data could not be read.</summary>
    public sealed record InvalidSeed : ClassificationFailure
    {
        internal InvalidSeed()
        {
        }
    }
}

namespace AStarDev.CloudSync.Classification;

/// <summary>How sure a signal is about a candidate.</summary>
public abstract record ClassificationConfidence
{
    private ClassificationConfidence()
    {
    }

    /// <summary>A clear match.</summary>
    public sealed record High : ClassificationConfidence
    {
        internal High()
        {
        }
    }

    /// <summary>An ambiguous match, for example a place name that is also a common word.</summary>
    public sealed record Low : ClassificationConfidence
    {
        internal Low()
        {
        }
    }
}

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Where the term list is in loading its terms.</summary>
public abstract record TermListLoadState
{
    private TermListLoadState()
    {
    }

    /// <summary>The terms are being read.</summary>
    public sealed record Loading : TermListLoadState;

    /// <summary>The terms were read.</summary>
    public sealed record Loaded : TermListLoadState;

    /// <summary>The terms, categories or file counts could not be read.</summary>
    public sealed record Failed : TermListLoadState;
}

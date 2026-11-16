namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Where a suggestions scan or a category read is.</summary>
public abstract record SuggestionsLoadState
{
    private SuggestionsLoadState()
    {
    }

    /// <summary>The scan or read is running.</summary>
    public sealed record Loading : SuggestionsLoadState;

    /// <summary>The scan or read finished.</summary>
    public sealed record Loaded : SuggestionsLoadState;

    /// <summary>The scan or read failed.</summary>
    public sealed record Failed : SuggestionsLoadState;

    /// <summary>The user cancelled the scan. This is a normal outcome, not an error.</summary>
    public sealed record Cancelled : SuggestionsLoadState;
}

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="SuggestionsLoadState"/> cases.</summary>
public static class SuggestionsLoadStateFactory
{
    /// <summary>Creates the <see cref="SuggestionsLoadState.Loading"/> case.</summary>
    public static SuggestionsLoadState CreateLoading() => new SuggestionsLoadState.Loading();

    /// <summary>Creates the <see cref="SuggestionsLoadState.Loaded"/> case.</summary>
    public static SuggestionsLoadState CreateLoaded() => new SuggestionsLoadState.Loaded();

    /// <summary>Creates the <see cref="SuggestionsLoadState.Failed"/> case.</summary>
    public static SuggestionsLoadState CreateFailed() => new SuggestionsLoadState.Failed();

    /// <summary>Creates the <see cref="SuggestionsLoadState.Cancelled"/> case.</summary>
    public static SuggestionsLoadState CreateCancelled() => new SuggestionsLoadState.Cancelled();
}

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="TermListLoadState"/> cases.</summary>
public static class TermListLoadStateFactory
{
    /// <summary>Creates the <see cref="TermListLoadState.Loading"/> case.</summary>
    public static TermListLoadState CreateLoading() => new TermListLoadState.Loading();

    /// <summary>Creates the <see cref="TermListLoadState.Loaded"/> case.</summary>
    public static TermListLoadState CreateLoaded() => new TermListLoadState.Loaded();

    /// <summary>Creates the <see cref="TermListLoadState.Failed"/> case.</summary>
    public static TermListLoadState CreateFailed() => new TermListLoadState.Failed();
}

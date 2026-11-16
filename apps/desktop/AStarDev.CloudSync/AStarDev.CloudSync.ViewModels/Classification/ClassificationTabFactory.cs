namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Creates <see cref="ClassificationTab"/> cases.</summary>
public static class ClassificationTabFactory
{
    /// <summary>Creates the <see cref="ClassificationTab.Terms"/> case.</summary>
    public static ClassificationTab CreateTerms() => new ClassificationTab.Terms();

    /// <summary>Creates the <see cref="ClassificationTab.Suggestions"/> case.</summary>
    public static ClassificationTab CreateSuggestions() => new ClassificationTab.Suggestions();
}

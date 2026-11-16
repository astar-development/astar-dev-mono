using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>One row of the term list.</summary>
public sealed class TermRowViewModel : LocalizedObservableObject
{
    internal TermRowViewModel(ClassificationTerm term, string categoryName, int fileCount, ILocalizationService localization) : base(localization) =>
        throw new NotImplementedException($"{term}{categoryName}{fileCount}");

    /// <summary>The term id.</summary>
    public TermId Id => throw new NotImplementedException();

    /// <summary>The term text.</summary>
    public string Text => throw new NotImplementedException();

    /// <summary>The category name.</summary>
    public string CategoryName => throw new NotImplementedException();

    /// <summary>The localised source.</summary>
    public string SourceText => throw new NotImplementedException();

    /// <summary>The number of files linked to the term.</summary>
    public int FileCount => throw new NotImplementedException();

    /// <summary>The file count formatted for the culture.</summary>
    public string FileCountText => throw new NotImplementedException();

    /// <summary>Whether the term is active or ignored.</summary>
    public TermStatus Status => throw new NotImplementedException();

    /// <summary>Whether the term is ignored.</summary>
    public bool IsIgnored => throw new NotImplementedException();

    /// <summary>The localised status.</summary>
    public string StatusText => throw new NotImplementedException();

    /// <summary>The label of the toggle button.</summary>
    public string ToggleText => throw new NotImplementedException();

    /// <summary>The localised error of the last toggle, or empty.</summary>
    public string ErrorText => throw new NotImplementedException();

    /// <summary>Whether the last toggle failed.</summary>
    public bool HasError => throw new NotImplementedException();

    internal Unit Apply(TermStatus status) => throw new NotImplementedException($"{status}");

    internal Unit Fail() => throw new NotImplementedException();
}

using AStarDev.CloudSync.Classification;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The classification window's term list.</summary>
public sealed class ClassificationTermListViewModel : LocalizedObservableObject
{
    /// <summary>Creates the list; it shows nothing until <see cref="LoadCommand"/> runs.</summary>
    /// <param name="termStore">Reads the terms and changes their status.</param>
    /// <param name="categoryStore">Reads the category names.</param>
    /// <param name="fileStore">Reads the file counts.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ClassificationTermListViewModel(ITermStore termStore, ICategoryStore categoryStore, IFileClassificationStore fileStore, ILocalizationService localization) : base(localization) =>
        throw new NotImplementedException($"{termStore}{categoryStore}{fileStore}");

    /// <summary>Where loading is.</summary>
    public TermListLoadState State => throw new NotImplementedException();

    /// <summary>The rows that match the filter, in sort order.</summary>
    public IReadOnlyList<TermRowViewModel> Rows => throw new NotImplementedException();

    /// <summary>The filter text. Rows whose term or category contain it, ignoring case, are listed.</summary>
    public string FilterText
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException($"{value}");
    }

    /// <summary>The sort column.</summary>
    public TermSortColumn SortColumn => throw new NotImplementedException();

    /// <summary>The sort direction.</summary>
    public TermSortDirection SortDirection => throw new NotImplementedException();

    /// <summary>Whether the terms are being read.</summary>
    public bool IsLoading => throw new NotImplementedException();

    /// <summary>Whether reading failed.</summary>
    public bool IsFailed => throw new NotImplementedException();

    /// <summary>Whether reading worked and there are no terms.</summary>
    public bool IsEmpty => throw new NotImplementedException();

    /// <summary>Whether there are terms but the filter hides them all.</summary>
    public bool HasNoMatches => throw new NotImplementedException();

    /// <summary>The list heading.</summary>
    public string TitleText => throw new NotImplementedException();

    /// <summary>The filter box placeholder.</summary>
    public string FilterPlaceholderText => throw new NotImplementedException();

    /// <summary>Shown while loading.</summary>
    public string LoadingText => throw new NotImplementedException();

    /// <summary>Shown when there are no terms.</summary>
    public string EmptyText => throw new NotImplementedException();

    /// <summary>Shown when the filter matches nothing.</summary>
    public string NoMatchesText => throw new NotImplementedException();

    /// <summary>Shown when reading failed.</summary>
    public string LoadFailedText => throw new NotImplementedException();

    /// <summary>The term column header.</summary>
    public string TermHeaderText => throw new NotImplementedException();

    /// <summary>The category column header.</summary>
    public string CategoryHeaderText => throw new NotImplementedException();

    /// <summary>The source column header.</summary>
    public string SourceHeaderText => throw new NotImplementedException();

    /// <summary>The file count column header.</summary>
    public string FileCountHeaderText => throw new NotImplementedException();

    /// <summary>The ignored column header.</summary>
    public string IgnoredHeaderText => throw new NotImplementedException();

    /// <summary>Reads the terms, categories and file counts.</summary>
    public IAsyncRelayCommand LoadCommand => throw new NotImplementedException();

    /// <summary>Sorts by the column; the same column again flips the direction.</summary>
    public IRelayCommand<TermSortColumn> SortCommand => throw new NotImplementedException();

    /// <summary>Ignores an active term or restores an ignored one. Never deletes.</summary>
    public IAsyncRelayCommand<TermRowViewModel> ToggleIgnoredCommand => throw new NotImplementedException();
}

using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Hosts the term list and the suggestions list as two tabs.</summary>
public sealed class ClassificationTabsViewModel : LocalizedObservableObject
{
    /// <summary>Creates the host.</summary>
    /// <param name="terms">The term list.</param>
    /// <param name="suggestions">The suggestions list.</param>
    /// <param name="localization">Supplies the text.</param>
    public ClassificationTabsViewModel(ClassificationTermListViewModel terms, ClassificationSuggestionsViewModel suggestions, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{nameof(terms)} {nameof(suggestions)}");

    /// <summary>The term list.</summary>
    public ClassificationTermListViewModel Terms => throw new NotImplementedException();

    /// <summary>The suggestions list.</summary>
    public ClassificationSuggestionsViewModel Suggestions => throw new NotImplementedException();

    /// <summary>The tab that is shown.</summary>
    public ClassificationTab Selected => throw new NotImplementedException();

    /// <summary>Whether the term list is shown.</summary>
    public bool IsTermsSelected => throw new NotImplementedException();

    /// <summary>Whether the suggestions list is shown.</summary>
    public bool IsSuggestionsSelected => throw new NotImplementedException();

    /// <summary>The terms tab header.</summary>
    public string TermsTabText => throw new NotImplementedException();

    /// <summary>The suggestions tab header.</summary>
    public string SuggestionsTabText => throw new NotImplementedException();

    /// <summary>Loads the term list, which is the tab shown first.</summary>
    public IAsyncRelayCommand LoadCommand => throw new NotImplementedException();

    /// <summary>Shows the term list, reloading it first when a word was promoted or ignored since it loaded.</summary>
    public IAsyncRelayCommand ShowTermsCommand => throw new NotImplementedException();

    /// <summary>Shows the suggestions list, scanning the first time only.</summary>
    public IAsyncRelayCommand ShowSuggestionsCommand => throw new NotImplementedException();
}

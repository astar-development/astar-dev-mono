using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Hosts the term list and the suggestions list as two tabs. The suggestions are scanned the first time their tab is shown, and the term list reloads when its tab is shown after a word was promoted or ignored.</summary>
public sealed class ClassificationTabsViewModel : LocalizedObservableObject
{
    private ClassificationTab selected = ClassificationTabFactory.CreateTerms();
    private bool suggestionsRequested;
    private bool termsStale;

    /// <summary>Creates the host showing the term list.</summary>
    /// <param name="terms">The term list.</param>
    /// <param name="suggestions">The suggestions list.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ClassificationTabsViewModel(ClassificationTermListViewModel terms, ClassificationSuggestionsViewModel suggestions, ILocalizationService localization) : base(localization)
    {
        Terms = terms;
        Suggestions = suggestions;
        suggestions.TermsChanged += (_, _) => termsStale = true;
        LoadCommand = new AsyncRelayCommand(() => terms.LoadCommand.ExecuteAsync(null));
        ShowTermsCommand = new AsyncRelayCommand(ShowTermsAsync);
        ShowSuggestionsCommand = new AsyncRelayCommand(ShowSuggestionsAsync);
    }

    /// <summary>The term list.</summary>
    public ClassificationTermListViewModel Terms { get; }

    /// <summary>The suggestions list.</summary>
    public ClassificationSuggestionsViewModel Suggestions { get; }

    /// <summary>The tab that is shown.</summary>
    public ClassificationTab Selected => selected;

    /// <summary>Whether the term list is shown.</summary>
    public bool IsTermsSelected => selected is ClassificationTab.Terms;

    /// <summary>Whether the suggestions list is shown.</summary>
    public bool IsSuggestionsSelected => selected is ClassificationTab.Suggestions;

    /// <summary>The terms tab header.</summary>
    public string TermsTabText => Terms.TitleText;

    /// <summary>The suggestions tab header.</summary>
    public string SuggestionsTabText => Suggestions.TitleText;

    /// <summary>Loads the term list, which is the tab shown first.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Shows the term list, reloading it first when a word was promoted or ignored since it loaded.</summary>
    public IAsyncRelayCommand ShowTermsCommand { get; }

    /// <summary>Shows the suggestions list, scanning the first time only.</summary>
    public IAsyncRelayCommand ShowSuggestionsCommand { get; }

    private async Task ShowTermsAsync()
    {
        Select(ClassificationTabFactory.CreateTerms());
        if (!termsStale)
            return;

        termsStale = false;
        await Terms.LoadCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    private async Task ShowSuggestionsAsync()
    {
        Select(ClassificationTabFactory.CreateSuggestions());
        if (suggestionsRequested)
            return;

        suggestionsRequested = true;
        await Suggestions.LoadCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    private void Select(ClassificationTab tab)
    {
        selected = tab;
        OnPropertyChanged(nameof(Selected));
        OnPropertyChanged(nameof(IsTermsSelected));
        OnPropertyChanged(nameof(IsSuggestionsSelected));
    }
}

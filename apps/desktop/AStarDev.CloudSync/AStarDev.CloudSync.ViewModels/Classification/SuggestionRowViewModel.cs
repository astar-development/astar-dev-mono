using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>One row of the suggestions list.</summary>
public sealed class SuggestionRowViewModel : LocalizedObservableObject
{
    private readonly ClassificationSuggestion suggestion;
    private bool hasError;

    internal SuggestionRowViewModel(ClassificationSuggestion suggestion, ILocalizationService localization) : base(localization) => this.suggestion = suggestion;

    /// <summary>The suggested word.</summary>
    public string Word => suggestion.Word;

    /// <summary>The number of files whose path contains the word.</summary>
    public int FileCount => suggestion.FileCount;

    /// <summary>The file count formatted for the culture.</summary>
    public string FileCountText => FileCount.ToString("N0", Localization.CurrentCulture);

    /// <summary>Whether the word is a known first name.</summary>
    public bool IsFirstName => suggestion.Hint is SuggestionHint.FirstName;

    /// <summary>The localised first-name badge, or empty when there is no hint.</summary>
    public string HintText => IsFirstName ? Localization.GetLocal("Classification.Suggestions.Hint.FirstName") : string.Empty;

    /// <summary>The label of the promote button.</summary>
    public string PromoteText => Localization.GetLocal("Classification.Suggestions.Promote");

    /// <summary>The label of the ignore button.</summary>
    public string IgnoreText => Localization.GetLocal("Classification.Suggestions.Ignore");

    /// <summary>The localised error of the last promote or ignore, or empty.</summary>
    public string ErrorText => hasError ? Localization.GetLocal("Classification.Suggestions.ActionFailed") : string.Empty;

    /// <summary>Whether the last promote or ignore failed.</summary>
    public bool HasError => hasError;

    internal Unit Fail()
    {
        hasError = true;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }

    internal Unit ClearError()
    {
        hasError = false;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorText));

        return Unit.Instance;
    }
}

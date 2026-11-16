using AStarDev.CloudSync.Classification;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>One row of the suggestions list.</summary>
public sealed class SuggestionRowViewModel : LocalizedObservableObject
{
    internal SuggestionRowViewModel(ClassificationSuggestion suggestion, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{nameof(suggestion)}");

    /// <summary>The suggested word.</summary>
    public string Word => throw new NotImplementedException();

    /// <summary>The number of files whose path contains the word.</summary>
    public int FileCount => throw new NotImplementedException();

    /// <summary>The file count formatted for the culture.</summary>
    public string FileCountText => throw new NotImplementedException();

    /// <summary>Whether the word is a known first name.</summary>
    public bool IsFirstName => throw new NotImplementedException();

    /// <summary>The localised first-name badge, or empty when there is no hint.</summary>
    public string HintText => throw new NotImplementedException();

    /// <summary>The label of the promote button.</summary>
    public string PromoteText => throw new NotImplementedException();

    /// <summary>The label of the ignore button.</summary>
    public string IgnoreText => throw new NotImplementedException();

    /// <summary>The localised error of the last promote or ignore, or empty.</summary>
    public string ErrorText => throw new NotImplementedException();

    /// <summary>Whether the last promote or ignore failed.</summary>
    public bool HasError => throw new NotImplementedException();
}

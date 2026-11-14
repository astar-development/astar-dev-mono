using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>The conflicts of one account.</summary>
public sealed class ConflictAccountGroupViewModel : LocalizedObservableObject
{
    internal ConflictAccountGroupViewModel(string accountName, IReadOnlyList<ConflictItemViewModel> items, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{accountName}{items}");

    /// <summary>The account's name.</summary>
    public string AccountName => throw new NotImplementedException(nameof(AccountName));

    /// <summary>The account's name and how many conflicts it has.</summary>
    public string HeaderText => throw new NotImplementedException(nameof(HeaderText));

    /// <summary>The account's conflicts.</summary>
    public IReadOnlyList<ConflictItemViewModel> Items => throw new NotImplementedException(nameof(Items));
}

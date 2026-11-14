using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>The conflicts of one account.</summary>
public sealed class ConflictAccountGroupViewModel : LocalizedObservableObject
{
    internal ConflictAccountGroupViewModel(string accountName, IReadOnlyList<ConflictItemViewModel> items, ILocalizationService localization) : base(localization)
    {
        AccountName = accountName;
        Items = items;
    }

    /// <summary>The account's name.</summary>
    public string AccountName { get; }

    /// <summary>The account's name and how many conflicts it has.</summary>
    public string HeaderText => Localization.GetLocal(CountKey.For("Conflict.Group", Items.Count), AccountName, Items.Count.ToString("N0", Localization.CurrentCulture));

    /// <summary>The account's conflicts.</summary>
    public IReadOnlyList<ConflictItemViewModel> Items { get; }
}

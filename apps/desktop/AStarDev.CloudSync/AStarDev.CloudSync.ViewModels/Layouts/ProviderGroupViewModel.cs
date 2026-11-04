using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>One provider and its accounts, as shown in the layout B sidebar tree.</summary>
/// <param name="provider">The provider.</param>
/// <param name="accounts">The provider's accounts, in overview order.</param>
/// <param name="localization">Supplies the culture for the account count.</param>
public sealed class ProviderGroupViewModel(ProviderViewModel provider, IReadOnlyList<AccountViewModel> accounts, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The provider.</summary>
    public ProviderViewModel Provider => provider;

    /// <summary>The provider's accounts.</summary>
    public IReadOnlyList<AccountViewModel> Accounts => accounts;

    /// <summary>The number of accounts, formatted for the culture.</summary>
    public string CountText => accounts.Count.ToString("N0", Localization.CurrentCulture);
}

using AStarDev.CloudSync.Accounts;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>A cloud provider as shown in a provider badge.</summary>
/// <param name="provider">The provider.</param>
/// <param name="localization">Supplies the provider's name and badge code.</param>
public sealed class ProviderViewModel(CloudProvider provider, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    private string NameKey => provider switch
    {
        CloudProvider.OneDrive => "Provider.OneDrive",
        _ => "Provider.Unknown"
    };

    /// <summary>The provider's name, e.g. "OneDrive".</summary>
    public string Name => Localization.GetLocal(NameKey);

    /// <summary>The short code shown in the badge, e.g. "OD".</summary>
    public string Code => Localization.GetLocal($"{NameKey}.Code");

    /// <summary>Whether this is OneDrive; selects the badge colour.</summary>
    public bool IsOneDrive => provider is CloudProvider.OneDrive;
}

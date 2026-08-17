using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Accounts;

/// <summary>Container-backed factory for <see cref="AccountCardViewModel"/> instances.</summary>
public sealed class AccountCardViewModelFactory(ILocalizationService localizationService) : IAccountCardViewModelFactory
{
    /// <inheritdoc />
    public AccountCardViewModel Create(OneDriveAccount account) => new(account, localizationService);
}

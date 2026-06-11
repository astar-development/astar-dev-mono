namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

/// <summary>Creates <see cref="AccountCardViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IAccountCardViewModelFactory
{
    /// <summary>Creates a card view model for the supplied account.</summary>
    AccountCardViewModel Create(OneDriveAccount account);
}

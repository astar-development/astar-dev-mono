namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

/// <summary>Creates <see cref="AccountFilesViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IAccountFilesViewModelFactory
{
    /// <summary>Creates a files view model for the supplied account.</summary>
    AccountFilesViewModel Create(OneDriveAccount account);
}

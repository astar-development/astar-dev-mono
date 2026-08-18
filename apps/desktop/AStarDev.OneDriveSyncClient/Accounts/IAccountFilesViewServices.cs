using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Accounts;

/// <summary>
///  View model for the "Files" tab of a <see cref="AccountTabViewModel"/>. Displays the account's OneDrive folder structure and allows the user to include/exclude folders for syncing.
/// </summary>
public interface IAccountFilesViewServices
{
    /// <summary>
    ///  The <see cref="IAuthService"/> instance used to acquire access tokens for the account.
    /// </summary>
    IAuthService AuthService { get; }
    /// <summary>
    ///   The <see cref="IAuthService"/> instance used to acquire access tokens for the account.
    /// </summary>
    ILocalizationService LocalizationService { get; }

    /// <summary>
    ///  The <see cref="IGraphService"/> instance used to query OneDrive for the account's folder structure.
    /// </summary>
    IGraphService GraphService { get; }

    /// <summary>
    ///  The <see cref="ISyncRuleService"/> instance used to retrieve and persist sync rules for the account.
    /// </summary>
    ISyncRuleService SyncRuleService { get; }
}

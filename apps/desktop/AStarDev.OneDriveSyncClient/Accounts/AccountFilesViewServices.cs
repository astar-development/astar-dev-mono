using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Accounts;

/// <inheritdoc />
public sealed record AccountFilesViewServices(IAuthService AuthService, ILocalizationService LocalizationService, IGraphService GraphService, ISyncRuleService SyncRuleService) : IAccountFilesViewServices;

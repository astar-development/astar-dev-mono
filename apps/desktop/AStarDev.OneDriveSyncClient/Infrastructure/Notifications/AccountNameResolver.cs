using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Accounts;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public sealed class AccountNameResolver(AccountsViewModel accountsViewModel) : IAccountNameResolver
{
    /// <inheritdoc />
    public string Resolve(string accountId) => accountsViewModel.Accounts.FirstOrDefault(card => card.Id == accountId)?.DisplayName ?? accountId;
}

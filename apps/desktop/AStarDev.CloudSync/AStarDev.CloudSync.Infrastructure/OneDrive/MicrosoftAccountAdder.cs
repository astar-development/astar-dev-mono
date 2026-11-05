using AStarDev.CloudSync.Core.Accounts;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Adds a personal Microsoft account: connects the token cache, signs in, reads the drive and saves the account. A new MSAL account is removed again if the attempt fails after sign-in.</summary>
/// <param name="tokenCache">Connects the token cache before sign-in.</param>
/// <param name="msal">Signs in and manages MSAL accounts.</param>
/// <param name="driveLookup">Reads the drive after sign-in.</param>
/// <param name="accountStore">Saves the account.</param>
/// <param name="logger">Reports failures, without tokens or account details.</param>
public sealed class MicrosoftAccountAdder(TokenCacheInitialiser tokenCache, IMsalClient msal, IDriveLookup driveLookup, IAccountStore accountStore, ILogger<MicrosoftAccountAdder> logger) : IAccountAdder
{
    /// <inheritdoc />
    public Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{tokenCache}{msal}{driveLookup}{accountStore}{logger}{cancellationToken}");
}

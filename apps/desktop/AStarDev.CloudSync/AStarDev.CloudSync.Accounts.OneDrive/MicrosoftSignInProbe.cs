using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Probes an account with a silent token request. Only MSAL's "UI required" marks it as needing sign-in; network, keyring or cache failures are unknown (KB: gotcha-msal-token-lifecycle-on-linux).</summary>
/// <param name="tokens">The silent token source.</param>
internal sealed class MicrosoftSignInProbe(IAccessTokenSource tokens) : IAccountSignInProbe
{
    /// <inheritdoc />
    public Task<Option<AccountSignInState>> ProbeAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{tokens}{accountId}{cancellationToken}");

    private static Option<AccountSignInState> ToState(AccessTokenOutcome outcome) => outcome switch
    {
        AccessTokenOutcome.Acquired => Option.Some(AccountSignInStateFactory.CreateSignedIn()),
        AccessTokenOutcome.SignInRequired => Option.Some(AccountSignInStateFactory.CreateSignInRequired()),
        _ => Option.None<AccountSignInState>(),
    };
}

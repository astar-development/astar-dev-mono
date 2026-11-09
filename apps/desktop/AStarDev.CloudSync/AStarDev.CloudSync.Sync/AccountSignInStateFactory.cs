namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AccountSignInState"/> cases.</summary>
public static class AccountSignInStateFactory
{
    /// <summary>Creates the signed-in case.</summary>
    public static AccountSignInState CreateSignedIn() => new AccountSignInState.SignedIn();

    /// <summary>Creates the sign-in-required case.</summary>
    public static AccountSignInState CreateSignInRequired() => new AccountSignInState.SignInRequired();
}

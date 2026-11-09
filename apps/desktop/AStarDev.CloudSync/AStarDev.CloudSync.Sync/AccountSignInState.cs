namespace AStarDev.CloudSync.Sync;

/// <summary>Whether an account can get tokens without the user. Derived at startup and when a token request fails; never stored.</summary>
public abstract record AccountSignInState
{
    private AccountSignInState()
    {
    }

    /// <summary>Tokens can be refreshed silently.</summary>
    public sealed record SignedIn : AccountSignInState
    {
        internal SignedIn()
        {
        }
    }

    /// <summary>The provider needs the user to sign in again; sync cannot run until then.</summary>
    public sealed record SignInRequired : AccountSignInState
    {
        internal SignInRequired()
        {
        }
    }
}

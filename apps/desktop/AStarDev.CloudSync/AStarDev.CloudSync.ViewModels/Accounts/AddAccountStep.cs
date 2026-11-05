using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Where the add-account dialog is.</summary>
public abstract record AddAccountStep
{
    private AddAccountStep()
    {
    }

    /// <summary>The dialog is closed.</summary>
    public sealed record Closed : AddAccountStep;

    /// <summary>The user is signing in through the browser.</summary>
    public sealed record SigningIn : AddAccountStep;

    /// <summary>The account was already added; it was signed in again and its settings were kept.</summary>
    public sealed record AlreadyAdded : AddAccountStep;

    /// <summary>The attempt failed.</summary>
    public sealed record Failed(AccountAddFailure Failure) : AddAccountStep;
}

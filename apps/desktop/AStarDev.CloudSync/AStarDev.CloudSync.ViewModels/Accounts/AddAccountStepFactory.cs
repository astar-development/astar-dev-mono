using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Creates <see cref="AddAccountStep"/> cases.</summary>
public static class AddAccountStepFactory
{
    /// <summary>Creates the closed step.</summary>
    public static AddAccountStep CreateClosed() => new AddAccountStep.Closed();

    /// <summary>Creates the signing-in step.</summary>
    public static AddAccountStep CreateSigningIn() => new AddAccountStep.SigningIn();

    /// <summary>Creates the already-added step.</summary>
    public static AddAccountStep CreateAlreadyAdded() => new AddAccountStep.AlreadyAdded();

    /// <summary>Creates the added step.</summary>
    /// <param name="accountId">The new account.</param>
    public static AddAccountStep CreateAdded(AccountId accountId) => new AddAccountStep.Added(accountId);

    /// <summary>Creates the failed step.</summary>
    /// <param name="failure">Why the attempt failed.</param>
    public static AddAccountStep CreateFailed(AccountAddFailure failure) => new AddAccountStep.Failed(failure);
}

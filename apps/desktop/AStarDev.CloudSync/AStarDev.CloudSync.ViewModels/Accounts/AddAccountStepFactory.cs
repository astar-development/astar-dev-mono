using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Creates <see cref="AddAccountStep"/> cases.</summary>
public static class AddAccountStepFactory
{
    /// <summary>Creates the closed step.</summary>
    public static AddAccountStep CreateClosed() => throw new NotImplementedException();

    /// <summary>Creates the signing-in step.</summary>
    public static AddAccountStep CreateSigningIn() => throw new NotImplementedException();

    /// <summary>Creates the already-added step.</summary>
    public static AddAccountStep CreateAlreadyAdded() => throw new NotImplementedException();

    /// <summary>Creates the failed step.</summary>
    /// <param name="failure">Why the attempt failed.</param>
    public static AddAccountStep CreateFailed(AccountAddFailure failure) => throw new NotImplementedException($"{failure}");
}

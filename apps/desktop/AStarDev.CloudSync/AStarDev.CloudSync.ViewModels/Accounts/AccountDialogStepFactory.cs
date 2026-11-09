namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Creates <see cref="AccountDialogStep"/> cases.</summary>
public static class AccountDialogStepFactory
{
    /// <summary>Creates the closed step.</summary>
    public static AccountDialogStep CreateClosed() => new AccountDialogStep.Closed();

    /// <summary>Creates the confirm-removal step.</summary>
    public static AccountDialogStep CreateConfirmingRemoval(AccountViewModel account) => new AccountDialogStep.ConfirmingRemoval(account);

    /// <summary>Creates the working step.</summary>
    public static AccountDialogStep CreateWorking(string? text) => new AccountDialogStep.Working(text ?? string.Empty);

    /// <summary>Creates the message step.</summary>
    public static AccountDialogStep CreateMessage(string? text) => new AccountDialogStep.Message(text ?? string.Empty);
}

namespace Fab4Kids.Web.Fulfilment;

/// <summary>Factory for <see cref="ResendOutcome"/>.</summary>
public static class ResendOutcomeFactory
{
    public static ResendOutcome Sent(int linkCount) => new ResendSent(linkCount);

    public static ResendOutcome EmailMismatch() => new ResendEmailMismatch();

    public static ResendOutcome Failed(string message) => new ResendFailed(message);
}

namespace Fab4Kids.Web.Fulfilment;

/// <summary>Factory for <see cref="FulfilmentOutcome"/>.</summary>
public static class FulfilmentOutcomeFactory
{
    public static FulfilmentOutcome Delivered(int linkCount) => new FulfilmentDelivered(linkCount);

    public static FulfilmentOutcome Duplicate() => new FulfilmentDuplicate();

    public static FulfilmentOutcome NoCustomerEmail() => new FulfilmentNoCustomerEmail();

    public static FulfilmentOutcome Failed(string message) => new FulfilmentFailed(message);
}

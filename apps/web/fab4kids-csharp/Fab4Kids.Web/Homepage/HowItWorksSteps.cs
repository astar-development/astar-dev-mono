namespace Fab4Kids.Web.Homepage;

/// <summary>The three steps shown in the homepage "How it works" section.</summary>
public static class HowItWorksSteps
{
    public static IReadOnlyList<HowItWorksStep> All { get; } =
    [
        HowItWorksStepFactory.Create(1, "#3B8FE0", "Browse & pick", "Filter by subject, key stage or format to find the perfect fit."),
        HowItWorksStepFactory.Create(2, "#E8483A", "Checkout securely", "Pay once, download instantly — no account required."),
        HowItWorksStepFactory.Create(3, "#4CAF6D", "Print & learn", "Print at home or edit the Word file to make it your own."),
    ];
}

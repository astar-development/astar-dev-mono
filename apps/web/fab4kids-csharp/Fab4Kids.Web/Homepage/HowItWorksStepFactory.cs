namespace Fab4Kids.Web.Homepage;

/// <summary>Factory for <see cref="HowItWorksStep"/>.</summary>
public static class HowItWorksStepFactory
{
    public static HowItWorksStep Create(int number, string color, string title, string description) => new(number, color, title, description);
}

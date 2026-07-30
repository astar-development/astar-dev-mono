namespace Fab4Kids.Web.Homepage;

/// <summary>A single numbered step in the homepage "How it works" section.</summary>
public sealed record HowItWorksStep(int Number, string Color, string Title, string Description);

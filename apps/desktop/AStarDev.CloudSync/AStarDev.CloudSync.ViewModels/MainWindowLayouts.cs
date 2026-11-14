using AStarDev.CloudSync.ViewModels.Layouts;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>The three layouts the main window can show.</summary>
public sealed class MainWindowLayouts(LayoutAViewModel layoutA, LayoutBViewModel layoutB, LayoutCViewModel layoutC)
{
    /// <summary>The LayoutA.</summary>
    public LayoutAViewModel LayoutA { get; } = layoutA;

    /// <summary>The LayoutB.</summary>
    public LayoutBViewModel LayoutB { get; } = layoutB;

    /// <summary>The LayoutC.</summary>
    public LayoutCViewModel LayoutC { get; } = layoutC;
}

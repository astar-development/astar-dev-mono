using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit;

internal static class ScreenReaderNames
{
    internal static IReadOnlyList<string> Unnamed(Visual root) => [.. root.GetVisualDescendants().OfType<Control>().Where(IsInteractive).Where(control => !HasName(control)).Select(Describe)];

    private static bool IsInteractive(Control control) => control is Button or TextBox or ComboBox or ListBox or ToggleButton or Slider && control.IsEffectivelyVisible && control.Name?.StartsWith("PART_", StringComparison.Ordinal) != true && control.FindAncestorOfType<ComboBox>() is null && control.FindAncestorOfType<ListBoxItem>() is null;

    private static bool HasName(Control control) => !string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)) || control is ContentControl { Content: string { Length: > 0 } } || control.GetVisualDescendants().OfType<TextBlock>().Any(text => !string.IsNullOrWhiteSpace(text.Text));

    private static string Describe(Control control) => $"{control.GetType().Name} {control.Name} {string.Join(' ', control.Classes)} in {control.FindAncestorOfType<UserControl>()?.GetType().Name}";
}

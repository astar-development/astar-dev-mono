namespace Fab4Kids.Web.Theming;

/// <summary>Factory for <see cref="ThemeOption"/>.</summary>
public static class ThemeOptionFactory
{
    public static ThemeOption Create(Theme id, string icon, string label, string ariaLabel) => new(id, icon, label, ariaLabel);
}

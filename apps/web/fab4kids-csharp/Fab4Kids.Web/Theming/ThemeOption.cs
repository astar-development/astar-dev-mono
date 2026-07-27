namespace Fab4Kids.Web.Theming;

/// <summary>A single button in the <c>ThemeSwitcher</c> component.</summary>
public sealed record ThemeOption(Theme Id, string Icon, string Label, string AriaLabel);

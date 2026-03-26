using System.Globalization;
using System.Resources;

namespace AStar.Dev.OneDriveSync.old.Localisation;

/// <summary>
/// AXAML markup extension that resolves a string from the embedded Strings.resx
/// using the en-GB locale (TI-03 / TI-04).
///
/// Usage in AXAML:
///   xmlns:loc="clr-namespace:AStar.Dev.OneDriveSync.old.Localisation"
///   Text="{loc:Localize MainWindow_Title}"
/// </summary>
public sealed class LocalizeExtension(string key)
{
    private static readonly ResourceManager ResourceManager = new(
        "AStar.Dev.OneDriveSync.old.Localisation.Strings",
        typeof(LocalizeExtension).Assembly);

    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");

    public string Key { get; } = key;

    public object ProvideValue(IServiceProvider serviceProvider)
        => ResourceManager.GetString(Key, EnGb) ?? Key;
}

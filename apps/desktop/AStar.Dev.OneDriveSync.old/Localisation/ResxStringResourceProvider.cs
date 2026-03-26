using System.Globalization;
using System.Resources;

namespace AStar.Dev.OneDriveSync.old.Localisation;

public sealed class ResxStringResourceProvider(ResourceManager resourceManager) : IStringResourceProvider
{
    public string? GetString(string key, CultureInfo culture)
        => resourceManager.GetString(key, culture);
}

using System.Globalization;

namespace AStar.Dev.OneDriveSync.Localisation;

public sealed class LocalisationService(IStringResourceProvider provider, CultureInfo culture) : ILocalisationService
{
    public string Culture => culture.Name;

    public string GetString(string key)
        => provider.GetString(key, culture) ?? key;
}

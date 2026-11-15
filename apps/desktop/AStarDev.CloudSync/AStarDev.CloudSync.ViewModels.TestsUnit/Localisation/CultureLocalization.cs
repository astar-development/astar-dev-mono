using System.Globalization;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Localisation;

internal sealed class CultureLocalization(ILocalizationService strings, CultureInfo culture) : ILocalizationService
{
    public CultureInfo CurrentCulture => culture;

    public IReadOnlyList<CultureInfo> AvailableCultures => strings.AvailableCultures;

    public event EventHandler<CultureInfo>? CultureChanged
    {
        add { }
        remove { }
    }

    public string GetLocal(string key) => strings.GetLocal(key);

    public string GetLocal(string key, params object[] args) => string.Format(culture, strings.GetLocal(key), args);

    public Task SetCultureAsync(CultureInfo cultureToUse, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Initialise(CultureInfo? requested = null)
    {
    }
}

using System.ComponentModel;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>
///     INPC wrapper around a single localised string key.  Subscribed to
///     <see cref="ILocalisationService.LocaleChanged" /> so that any binding pointing
///     at <see cref="Value" /> updates automatically when the locale changes.
/// </summary>
internal sealed class LocalisedStringSource : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly ILocalisationService _service;

    internal LocalisedStringSource(string key, ILocalisationService service)
    {
        _key     = key;
        _service = service;
        _service.LocaleChanged += OnLocaleChanged;
    }

    /// <summary>The current localised value for the bound key.</summary>
    public string Value => _service.GetString(_key);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnLocaleChanged(object? sender, EventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

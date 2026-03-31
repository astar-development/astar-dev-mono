using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>
///     AXAML markup extension that resolves strings from <c>Strings.resx</c> and
///     updates in-place when the active locale changes (AC LO-03).
///
///     Usage:
///     <code>
///         xmlns:loc="using:AStar.Dev.OneDriveSync.Infrastructure.Localisation"
///         Text="{loc:Localise Settings_Heading}"
///     </code>
///
///     Implementation notes:
///     — Returns a <see cref="Binding" /> whose source is a <see cref="LocalisedStringSource" />.
///       The source implements <see cref="System.ComponentModel.INotifyPropertyChanged" /> and
///       re-fires on <see cref="ILocalisationService.LocaleChanged" />, so every bound control
///       updates without navigation.
///     — Falls back to the key itself when the service is not yet initialised (design-time).
/// </summary>
public sealed class LocaliseExtension : MarkupExtension
{
    /// <summary>Initialises the extension with the resource key.</summary>
    public LocaliseExtension(string key) => Key = key;

    /// <summary>The resource key to look up in <c>Strings.resx</c>.</summary>
    public string Key { get; set; }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var service = LocalisationServiceLocator.Instance;

        if (service is null)
            return Key;

        var source = new LocalisedStringSource(Key, service);

        return new Binding("Value") { Source = source, Mode = BindingMode.OneWay };
    }
}

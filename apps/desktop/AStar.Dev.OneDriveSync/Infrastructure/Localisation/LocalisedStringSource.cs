using System;
using System.ComponentModel;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>
///     INPC wrapper around a single localised string key.  Subscribed to
///     <see cref="ILocalisationService.LocaleChanged" /> via a <see cref="WeakReference" />
///     so that the source can be garbage-collected when the binding is released, preventing the
///     singleton service from rooting orphaned instances indefinitely.
/// </summary>
internal sealed class LocalisedStringSource : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly ILocalisationService _service;

    internal LocalisedStringSource(string key, ILocalisationService service)
    {
        _key     = key;
        _service = service;

        var weakSelf = new WeakReference<LocalisedStringSource>(this);
        service.LocaleChanged += (_, _) =>
        {
            if (weakSelf.TryGetTarget(out var target))
                target.PropertyChanged?.Invoke(target, new PropertyChangedEventArgs(nameof(Value)));
        };
    }

    /// <summary>The current localised value for the bound key.</summary>
    public string Value => _service.GetString(_key);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}

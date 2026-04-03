using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>Localises strings from embedded <c>Strings.resx</c> and persists locale selection to <see cref="AppSettings" /> (AC LO-01 to LO-04).</summary>
internal sealed partial class LocalisationService(IAppSettingsRepository settingsRepository, ILogger<LocalisationService> logger) : ILocalisationService
{
    private static readonly ResourceManager ResourceManager = new(
        "AStar.Dev.OneDriveSync.Infrastructure.Localisation.Strings",
        typeof(LocalisationService).Assembly);

    private CultureInfo _currentCulture = CultureInfo.GetCultureInfo("en-GB");

    /// <inheritdoc />
    public string CurrentLocale => _currentCulture.Name;

    /// <inheritdoc />
    public IReadOnlySet<string> SupportedLocales { get; } = FrozenSet.Create("en-GB");

    /// <inheritdoc />
    public event EventHandler? LocaleChanged;

    /// <inheritdoc />
    public string GetString(string key) => ResourceManager.GetString(key, _currentCulture) ?? key;

    /// <inheritdoc />
    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        var result = await settingsRepository.GetAsync(ct).ConfigureAwait(false);

        string locale = result is Result<AppSettings?, ErrorResponse>.Ok { Value.Locale: { } persisted }
            ? persisted
            : ResolveFirstLaunchLocale();

        ApplyLocale(locale);
        LogLocaleInitialised(logger, locale);
    }

    private string ResolveFirstLaunchLocale()
    {
        string osLocale = CultureInfo.CurrentUICulture.Name;

        if (SupportedLocales.Contains(osLocale))
            return osLocale;

        LogOsLocaleNotSupported(logger, osLocale);

        return "en-GB";
    }

    /// <inheritdoc />
    public async Task<Result<string, ErrorResponse>> SetLocaleAsync(string locale, CancellationToken ct = default)
    {
        if (!SupportedLocales.Contains(locale))
            return new Result<string, ErrorResponse>.Error(new ErrorResponse($"Locale '{locale}' is not supported."));

        var getResult = await settingsRepository.GetAsync(ct).ConfigureAwait(false);

        var settings = getResult is Result<AppSettings?, ErrorResponse>.Ok { Value: { } existing }
            ? existing
            : new AppSettings { Id = AppSettings.SingletonId };

        settings.Locale = locale;

        var saveResult = await settingsRepository.SaveAsync(settings, ct).ConfigureAwait(false);

        if (saveResult is Result<AppSettings, ErrorResponse>.Error err)
            return new Result<string, ErrorResponse>.Error(err.Reason);

        ApplyLocale(locale);
        LogLocaleChanged(logger, locale);

        return new Result<string, ErrorResponse>.Ok(locale);
    }

    private void ApplyLocale(string locale)
    {
        string resolved = SupportedLocales.Contains(locale) ? locale : "en-GB";
        _currentCulture = CultureInfo.GetCultureInfo(resolved);

        Dispatcher.UIThread.Post(() =>
        {
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.CurrentCulture   = _currentCulture;
        });

        LocaleChanged?.Invoke(this, EventArgs.Empty);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Locale initialised to {Locale}")]
    private static partial void LogLocaleInitialised(MelILogger logger, string locale);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Locale changed to {Locale}")]
    private static partial void LogLocaleChanged(MelILogger logger, string locale);

    [LoggerMessage(Level = LogLevel.Information, Message = "OS locale {OsLocale} is not supported; applying en-GB silently")]
    private static partial void LogOsLocaleNotSupported(MelILogger logger, string osLocale);
}

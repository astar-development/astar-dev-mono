namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>
///     Static accessor that gives AXAML markup extensions access to <see cref="ILocalisationService" />
///     without constructor injection — set once during app startup, before any AXAML is evaluated.
/// </summary>
internal static class LocalisationServiceLocator
{
    /// <summary>
    ///     The active service instance.  Set by <c>App.axaml.cs</c> after DI is built and
    ///     <see cref="ILocalisationService.InitialiseAsync" /> has completed.
    /// </summary>
    internal static ILocalisationService? Instance { get; set; }
}

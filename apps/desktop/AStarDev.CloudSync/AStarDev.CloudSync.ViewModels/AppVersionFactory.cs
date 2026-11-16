namespace AStarDev.CloudSync.ViewModels;

/// <summary>Creates <see cref="AppVersion"/> values.</summary>
public static class AppVersionFactory
{
    private const string LocalFallback = "0.1.0";

    /// <summary>Creates the version from the assembly's informational version. A leading "v" and any "+build" metadata are dropped; a missing or empty version becomes the local fallback <c>0.1.0</c>.</summary>
    /// <param name="informationalVersion">The raw version text, for example "v1.71.1+abc123".</param>
    public static AppVersion Create(string? informationalVersion)
    {
        var withoutMetadata = (informationalVersion ?? string.Empty).Split('+')[0].Trim().TrimStart('v', 'V');

        return new AppVersion(withoutMetadata.Length == 0 ? LocalFallback : withoutMetadata);
    }
}

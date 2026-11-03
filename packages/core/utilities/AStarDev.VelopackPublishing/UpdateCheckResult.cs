using global::Velopack;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>The outcome of an update check.</summary>
public abstract record UpdateCheckResult;

/// <summary>A newer version is available.</summary>
/// <param name="Info">The update that was found.</param>
public sealed record UpdateFound(UpdateInfo Info) : UpdateCheckResult;

/// <summary>The installed version is the latest.</summary>
public sealed record UpToDate : UpdateCheckResult;

/// <summary>The app is not running as a Velopack-installed instance, so it cannot be updated.</summary>
public sealed record NotInstalled : UpdateCheckResult;

/// <summary>The check could not be completed.</summary>
/// <param name="Reason">Why the check failed.</param>
public sealed record CheckFailed(string Reason) : UpdateCheckResult;

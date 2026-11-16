using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Velopack.Publishing;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Autostart;

/// <inheritdoc />
/// <param name="updates">Tells whether this copy was installed by Velopack.</param>
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the installed-build check and the process path.")]
internal sealed class VelopackAutostartLauncher(IVelopackUpdateService updates) : IAutostartLauncher
{
    /// <inheritdoc />
    public Option<string> LauncherPath => AutostartLauncherPath.Resolve(updates.IsInstalled, Environment.ProcessPath, Environment.GetEnvironmentVariable("APPIMAGE"));
}

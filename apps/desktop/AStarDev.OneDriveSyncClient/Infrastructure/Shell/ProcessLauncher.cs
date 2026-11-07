using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over Process.Start, which launches a real operating-system process.")]
public sealed class ProcessLauncher : IProcessLauncher
{
    /// <inheritdoc />
    public void Start(ProcessStartInfo startInfo) => _ = Process.Start(startInfo);
}

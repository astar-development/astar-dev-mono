using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the operating system's network events, which tests cannot raise.")]
internal sealed class SystemNetworkMonitor : INetworkMonitor, IDisposable
{
    /// <summary>Starts listening to the operating system.</summary>
    public SystemNetworkMonitor() => NetworkChange.NetworkAvailabilityChanged += OnAvailabilityChanged;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public NetworkAvailability Availability => NetworkInterface.GetIsNetworkAvailable() ? NetworkAvailabilityFactory.CreateAvailable() : NetworkAvailabilityFactory.CreateUnavailable();

    /// <summary>Stops listening to the operating system.</summary>
    public void Dispose() => NetworkChange.NetworkAvailabilityChanged -= OnAvailabilityChanged;

    private void OnAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) => Changed?.Invoke(this, EventArgs.Empty);
}

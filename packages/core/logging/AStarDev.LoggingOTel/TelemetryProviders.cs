using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace AStarDev.LoggingOTel;

/// <summary>The running metric and trace providers started by <see cref="OTelTelemetryConfigurator" />.</summary>
public sealed class TelemetryProviders : IDisposable
{
    private readonly MeterProvider meters;
    private readonly TracerProvider tracer;

    internal TelemetryProviders(MeterProvider meters, TracerProvider tracer)
    {
        this.meters = meters;
        this.tracer = tracer;
    }

    /// <summary>Flushes and stops both providers.</summary>
    public void Dispose()
    {
        meters.Dispose();
        tracer.Dispose();
    }
}

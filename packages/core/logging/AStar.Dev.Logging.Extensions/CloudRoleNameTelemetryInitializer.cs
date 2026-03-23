using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     A telemetry initializer responsible for setting the Cloud Role Name
///     and Instrumentation Key in the telemetry context.
/// </summary>
/// <param name="roleOrApplicationName">The Role / Application Name to configure Application Insights with</param>
/// <param name="instrumentationKey">The Instrumentation Key to configure Application Insights with</param>
public sealed class CloudRoleNameTelemetryInitializer(string roleOrApplicationName, string instrumentationKey) : ITelemetryInitializer
{
    /// <inheritdoc />
    public void Initialize(ITelemetry telemetry)
    {
        if(telemetry == null)
        {
            return;
        }

        // Set RoleName to provided value if not already set or is null/empty, otherwise set to empty string if still null
        if(string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = roleOrApplicationName ?? string.Empty;
        }

        // Set InstrumentationKey to provided value if not already set or is null/empty, otherwise set to empty string if still null
        if(string.IsNullOrEmpty(telemetry.Context.InstrumentationKey))
        {
            telemetry.Context.InstrumentationKey = instrumentationKey ?? string.Empty;
        }
    }
}
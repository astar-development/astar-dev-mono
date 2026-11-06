using Microsoft.Extensions.Logging;

namespace AStarDev.Web.Packages;

internal static partial class NugetPackageLog
{
    [LoggerMessage(EventId = 6200, Level = LogLevel.Warning, Message = "NuGet API unreachable for {PackageId}; using the last known good data.")]
    public static partial void ApiUnreachableUsingLastKnownGood(ILogger logger, string packageId);

    [LoggerMessage(EventId = 6201, Level = LogLevel.Error, Message = "NuGet API unreachable for {PackageId} and no cached data is available.")]
    public static partial void ApiUnreachableWithNoCachedData(ILogger logger, string packageId);
}

namespace AStarDev.CloudSync.Sync;

/// <summary>The pause limits <see cref="IAccountThrottleGate"/> applies (owner decision, #1460).</summary>
public static class ThrottlePolicy
{
    /// <summary>The first pause after a throttle that carried no Retry-After.</summary>
    public static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(5);

    /// <summary>The longest pause after consecutive throttles without Retry-After.</summary>
    public static readonly TimeSpan MaximumBackoff = TimeSpan.FromMinutes(5);

    /// <summary>The longest pause a provider's Retry-After can ask for.</summary>
    public static readonly TimeSpan MaximumRetryAfter = TimeSpan.FromMinutes(10);

    /// <summary>How many throttles within <see cref="HintWindow"/> make the overview suggest a lower concurrency (owner decision, #1463; configurable after the MVP, #1749).</summary>
    public const int HintThrottleCount = 10;

    /// <summary>The rolling window in which <see cref="HintThrottleCount"/> throttles make the overview suggest a lower concurrency.</summary>
    public static readonly TimeSpan HintWindow = TimeSpan.FromMinutes(10);
}

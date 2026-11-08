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
}

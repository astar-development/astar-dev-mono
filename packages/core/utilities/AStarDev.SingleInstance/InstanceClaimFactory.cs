namespace AStarDev.SingleInstance;

/// <summary>Creates <see cref="InstanceClaim"/> cases.</summary>
public static class InstanceClaimFactory
{
    /// <summary>Creates the primary case.</summary>
    public static InstanceClaim CreatePrimary(PrimaryInstance instance) => new InstanceClaim.Primary(instance);

    /// <summary>Creates the handed-off case.</summary>
    public static InstanceClaim CreateHandedOff() => new InstanceClaim.HandedOff();

    /// <summary>Creates the not-answered case.</summary>
    public static InstanceClaim CreateNotAnswered() => new InstanceClaim.NotAnswered();

    /// <summary>Creates the lock-unavailable case.</summary>
    public static InstanceClaim CreateLockUnavailable() => new InstanceClaim.LockUnavailable();
}

namespace AStarDev.CloudSync.ViewModels.SyncPairs;

/// <summary>Creates <see cref="SyncPairRemoveStep"/> cases.</summary>
public static class SyncPairRemoveStepFactory
{
    /// <summary>Creates the idle step.</summary>
    public static SyncPairRemoveStep CreateIdle() => new SyncPairRemoveStep.Idle();

    /// <summary>Creates the confirming step.</summary>
    public static SyncPairRemoveStep CreateConfirming() => new SyncPairRemoveStep.Confirming();
}

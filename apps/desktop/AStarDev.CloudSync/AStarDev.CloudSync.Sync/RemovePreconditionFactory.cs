namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemovePrecondition"/> cases.</summary>
public static class RemovePreconditionFactory
{
    /// <summary>Creates the <see cref="RemovePrecondition.MustMatch"/> case.</summary>
    public static RemovePrecondition CreateMustMatch(string? eTag) => new RemovePrecondition.MustMatch(eTag ?? string.Empty);
}

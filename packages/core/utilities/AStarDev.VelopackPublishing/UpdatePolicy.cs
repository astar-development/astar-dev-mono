namespace AStar.Dev.Velopack.Publishing;

/// <inheritdoc />
public sealed class UpdatePolicy : IUpdatePolicy
{
    /// <inheritdoc />
    public UpdateAction Decide(UpdateCheckResult result, UpdateDeferral deferral, DateTimeOffset now) => UpdateActionFactory.CreateNone();
}

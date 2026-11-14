using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The error a resolution returns when it cannot take the account's sync lease.</summary>
internal static class ResolutionRefusal
{
    internal const string Busy = "A sync pass is running for this account. Try again when it has finished.";

    internal const string Draining = "Sync is paused for an update or an account removal. Try again later.";

    internal static ErrorResponse For(SyncLockAttempt attempt) => new(attempt is SyncLockAttempt.Busy ? Busy : Draining);
}

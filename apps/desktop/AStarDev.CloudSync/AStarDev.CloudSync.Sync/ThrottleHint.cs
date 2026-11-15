namespace AStarDev.CloudSync.Sync;

/// <summary>Whether the overview should suggest a lower concurrency for an account (#1463).</summary>
public abstract record ThrottleHint
{
    private ThrottleHint()
    {
    }

    /// <summary>The account is not being throttled often; nothing to suggest.</summary>
    public sealed record Quiet : ThrottleHint
    {
        internal Quiet()
        {
        }
    }

    /// <summary>The provider throttled the account repeatedly; suggest fewer transfers at once.</summary>
    public sealed record SuggestLowerConcurrency : ThrottleHint
    {
        internal SuggestLowerConcurrency()
        {
        }
    }
}

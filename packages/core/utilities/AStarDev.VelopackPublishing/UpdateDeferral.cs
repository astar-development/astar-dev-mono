namespace AStar.Dev.Velopack.Publishing;

/// <summary>Whether the user deferred the update that is waiting (#1412). It is what the update policy needs to know about the user's earlier choices.</summary>
public abstract record UpdateDeferral
{
    internal UpdateDeferral()
    {
    }
}

/// <summary>The user has not deferred this update.</summary>
public sealed record NotDeferred : UpdateDeferral
{
    internal NotDeferred()
    {
    }
}

/// <summary>The user chose to install later.</summary>
public sealed record DeferredSince : UpdateDeferral
{
    internal DeferredSince(string version, DateTimeOffset firstDeferredAt, DateTimeOffset lastPromptedAt)
    {
        Version = version;
        FirstDeferredAt = firstDeferredAt;
        LastPromptedAt = lastPromptedAt;
    }

    /// <summary>The version that was deferred. A newer version starts again.</summary>
    public string Version { get; }

    /// <summary>When the user first deferred this version.</summary>
    public DateTimeOffset FirstDeferredAt { get; }

    /// <summary>When the user was last asked.</summary>
    public DateTimeOffset LastPromptedAt { get; }
}

namespace AStar.Dev.Velopack.Publishing;

/// <summary>What to do about an update check result (#1412).</summary>
public abstract record UpdateAction
{
    internal UpdateAction()
    {
    }
}

/// <summary>Nothing: there is no update, or the build cannot update.</summary>
public sealed record NoAction : UpdateAction
{
    internal NoAction()
    {
    }
}

/// <summary>Ask the user now: install or defer.</summary>
public sealed record PromptUser : UpdateAction
{
    internal PromptUser()
    {
    }
}

/// <summary>The user deferred recently: ask again at <see cref="At"/>.</summary>
public sealed record RemindLater : UpdateAction
{
    internal RemindLater(DateTimeOffset at) => At = at;

    /// <summary>When to ask again.</summary>
    public DateTimeOffset At { get; }
}

/// <summary>The user deferred for too long: warn, then install.</summary>
public sealed record ForceAfterWarning : UpdateAction
{
    internal ForceAfterWarning()
    {
    }
}

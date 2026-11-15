namespace AStar.Dev.Velopack.Publishing;

/// <summary>Creates <see cref="UpdateAction"/> cases.</summary>
public static class UpdateActionFactory
{
    /// <summary>Creates the <see cref="NoAction"/> case.</summary>
    public static UpdateAction CreateNone() => new NoAction();

    /// <summary>Creates the <see cref="PromptUser"/> case.</summary>
    public static UpdateAction CreatePrompt() => new PromptUser();

    /// <summary>Creates the <see cref="RemindLater"/> case.</summary>
    /// <param name="at">When to ask again.</param>
    public static UpdateAction CreateRemindLater(DateTimeOffset at) => new RemindLater(at);

    /// <summary>Creates the <see cref="ForceAfterWarning"/> case.</summary>
    public static UpdateAction CreateForceAfterWarning() => new ForceAfterWarning();
}

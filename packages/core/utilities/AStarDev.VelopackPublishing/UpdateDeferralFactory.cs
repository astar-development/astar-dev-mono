namespace AStar.Dev.Velopack.Publishing;

/// <summary>Creates <see cref="UpdateDeferral"/> cases.</summary>
public static class UpdateDeferralFactory
{
    /// <summary>Creates the <see cref="NotDeferred"/> case.</summary>
    public static UpdateDeferral CreateNotDeferred() => new NotDeferred();

    /// <summary>Creates the <see cref="DeferredSince"/> case. A blank version becomes empty text, so it never matches a real version.</summary>
    /// <param name="version">The version that was deferred.</param>
    /// <param name="firstDeferredAt">When the user first deferred it.</param>
    /// <param name="lastPromptedAt">When the user was last asked. It cannot be before the first deferral.</param>
    public static UpdateDeferral CreateDeferredSince(string? version, DateTimeOffset firstDeferredAt, DateTimeOffset lastPromptedAt) =>
        new DeferredSince(version ?? string.Empty, firstDeferredAt, lastPromptedAt < firstDeferredAt ? firstDeferredAt : lastPromptedAt);
}

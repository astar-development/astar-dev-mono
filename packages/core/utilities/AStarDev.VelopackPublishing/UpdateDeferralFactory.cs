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

    /// <summary>Creates the deferral to keep after the user deferred <paramref name="version"/> at <paramref name="now"/>. Deferring the same version again keeps the first time, so the seven days run from the first deferral; a different version starts again.</summary>
    /// <param name="previous">What was deferred before.</param>
    /// <param name="version">The version the user has just deferred.</param>
    /// <param name="now">When the user deferred it.</param>
    public static UpdateDeferral CreateAfterDeferring(UpdateDeferral previous, string version, DateTimeOffset now) =>
        previous is DeferredSince deferred && deferred.Version == version ? CreateDeferredSince(version, deferred.FirstDeferredAt, now) : CreateDeferredSince(version, now, now);
}

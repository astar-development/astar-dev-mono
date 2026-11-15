namespace AStar.Dev.Velopack.Publishing;

internal sealed record StoredUpdateDeferral(string Version, DateTimeOffset FirstDeferredAt, DateTimeOffset LastPromptedAt);

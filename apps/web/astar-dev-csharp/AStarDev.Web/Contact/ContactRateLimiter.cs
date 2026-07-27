using System.Collections.Concurrent;

namespace AStarDev.Web.Contact;

/// <inheritdoc cref="IContactRateLimiter"/>
/// <remarks>In-memory only — sufficient for a single-instance deployment, matching the previous Astro site's limiter.</remarks>
public sealed class ContactRateLimiter(TimeProvider timeProvider) : IContactRateLimiter
{
    private const int MaxRequestsPerWindow = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, RateLimitEntry> entries = new();

    public bool TryAcquire(string ipAddress)
    {
        var now = timeProvider.GetUtcNow();

        var entry = entries.AddOrUpdate(
            ipAddress,
            _ => new RateLimitEntry(1, now + Window),
            (_, existing) => now > existing.ResetAt ? new RateLimitEntry(1, now + Window) : existing with { Count = existing.Count + 1 });

        return entry.Count <= MaxRequestsPerWindow;
    }

    private sealed record RateLimitEntry(int Count, DateTimeOffset ResetAt);
}

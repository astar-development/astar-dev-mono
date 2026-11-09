using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.TestHelpers.Common;

public static class TestClock
{
    public static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider CreateTime() => new(Now);
}

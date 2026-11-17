using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class DelayRecordingTimeProvider : FakeTimeProvider
{
    private readonly Channel<TimeSpan> delays = Channel.CreateUnbounded<TimeSpan>();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _ = delays.Writer.TryWrite(dueTime);

        return timer;
    }

    internal async Task<TimeSpan> NextDelayAsync(CancellationToken cancellationToken) => await delays.Reader.ReadAsync(cancellationToken);
}

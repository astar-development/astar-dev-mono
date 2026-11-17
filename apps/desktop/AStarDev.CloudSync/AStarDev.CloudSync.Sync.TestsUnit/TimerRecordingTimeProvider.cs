using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

internal sealed class TimerRecordingTimeProvider : FakeTimeProvider
{
    private readonly Channel<TimeSpan> dueTimes = Channel.CreateUnbounded<TimeSpan>();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _ = dueTimes.Writer.TryWrite(dueTime);

        return timer;
    }

    public async Task<TimeSpan> NextTimerAsync(CancellationToken cancellationToken) => await dueTimes.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
}

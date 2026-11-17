using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestsUnit.Common;

public sealed class GivenARecordingLogger
{
    private const int Tasks = 50;
    private const int LogsPerTask = 200;

    [Fact]
    public async Task when_many_threads_log_at_once_then_every_entry_is_recorded_and_none_throws()
    {
        var logger = new RecordingLogger<GivenARecordingLogger>();
        var ct = TestContext.Current.CancellationToken;

        await Task.WhenAll(Enumerable.Range(0, Tasks).Select(task => Task.Run(() =>
        {
            var state = new List<KeyValuePair<string, object?>> { new("Key", task) };
            for(var index = 0; index < LogsPerTask; index++)
            {
                logger.Log(LogLevel.Information, new EventId(1, "Event"), state, null, (_, _) => "message");
                _ = logger.Messages.Count;
            }
        }, ct)));

        logger.Entries.Count.ShouldBe(Tasks * LogsPerTask);
        logger.Messages.Count.ShouldBe(Tasks * LogsPerTask);
        logger.Events.Count.ShouldBe(Tasks * LogsPerTask);
    }

    [Fact]
    public async Task when_many_threads_begin_scopes_at_once_then_every_scope_is_recorded()
    {
        var logger = new RecordingLogger<GivenARecordingLogger>();
        var ct = TestContext.Current.CancellationToken;

        await Task.WhenAll(Enumerable.Range(0, Tasks).Select(task => Task.Run(() =>
        {
            for(var index = 0; index < LogsPerTask; index++)
            {
                logger.BeginScope(new List<KeyValuePair<string, object?>> { new("Key", index) });
            }
        }, ct)));

        logger.Scopes.Count.ShouldBe(Tasks * LogsPerTask);
    }
}

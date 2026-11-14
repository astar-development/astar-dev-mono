using System.Collections.Concurrent;
using AStarDev.CloudSync.TestHelpers.Common;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

public sealed class GivenAConflictListLoadedOffTheUiThread
{
    [Fact]
    public async Task when_the_read_finishes_on_a_pool_thread_then_property_changes_are_still_raised_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var fixture = new ConflictListFixture();
        fixture.Reader.Add(ConflictListFixture.Photos.Id, AttentionItems.CreateConflict(ConflictListFixture.Photos.Id, "Camera/1.jpg"));
        fixture.Reader.Delay = async () => await Task.Delay(20, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var threads = new ConcurrentBag<int>();
        fixture.List.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        await ui.RunAsync(() => fixture.List.LoadAsync(TestContext.Current.CancellationToken)).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
    }
}

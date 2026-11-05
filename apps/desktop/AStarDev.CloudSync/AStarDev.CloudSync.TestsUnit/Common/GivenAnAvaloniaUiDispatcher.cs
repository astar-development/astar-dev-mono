using AStarDev.CloudSync.Common;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace AStarDev.CloudSync.TestsUnit.Common;

public class GivenAnAvaloniaUiDispatcher
{
    [AvaloniaFact]
    public async Task when_work_is_invoked_from_a_background_thread_then_it_runs_on_the_ui_thread()
    {
        var ranOnUiThread = false;

        await Task.Run(() => new AvaloniaUiDispatcher().InvokeAsync(() => ranOnUiThread = Dispatcher.UIThread.CheckAccess()), TestContext.Current.CancellationToken);

        ranOnUiThread.ShouldBeTrue();
    }
}

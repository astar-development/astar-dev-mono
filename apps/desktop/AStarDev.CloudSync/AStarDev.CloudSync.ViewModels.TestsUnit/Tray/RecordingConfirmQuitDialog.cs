using AStarDev.CloudSync.ViewModels.Tray;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

internal sealed class RecordingConfirmQuitDialog(QuitChoice choice) : IConfirmQuitDialog
{
    public int Asked { get; private set; }

    public Task<QuitChoice> AskAsync(CancellationToken cancellationToken)
    {
        Asked++;

        return Task.FromResult(choice);
    }
}

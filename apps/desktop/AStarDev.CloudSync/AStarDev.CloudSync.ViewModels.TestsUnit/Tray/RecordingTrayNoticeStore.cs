using AStarDev.CloudSync.ViewModels.Tray;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

internal sealed class RecordingTrayNoticeStore(TrayNoticeState initial) : ITrayNoticeStore
{
    public TrayNoticeState State { get; private set; } = initial;

    public int Marked { get; private set; }

    public TrayNoticeState Read() => State;

    public void MarkShown()
    {
        Marked++;
        State = TrayNoticeStateFactory.CreateShown();
    }
}

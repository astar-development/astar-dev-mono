using ReactiveUI;

namespace AStar.Dev.File.App.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    protected CancellationTokenSource CancellationTokenSource { get; set; } = new();

    public void Dispose()
    {
        CancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}

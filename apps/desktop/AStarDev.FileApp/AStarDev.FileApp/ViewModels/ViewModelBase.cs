using System.Reactive.Disposables;
using ReactiveUI;

namespace AStar.Dev.File.App.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    protected CancellationTokenSource CancellationTokenSource { get; set; } = new();
    protected CompositeDisposable Disposables { get; } = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disposables.Dispose();
            CancellationTokenSource.Dispose();
        }
    }
}

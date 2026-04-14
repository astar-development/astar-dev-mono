using ReactiveUI;
using System.Reactive.Disposables;

namespace AStar.Dev.File.App.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    protected CancellationTokenSource CancellationTokenSource { get; set; } = new();
    protected CompositeDisposable Disposables { get; } = [];

    public virtual void Dispose()
    {
        Disposables.Dispose();
        CancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}

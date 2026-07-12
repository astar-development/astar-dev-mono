using System.Windows.Input;

namespace AStar.Dev.Wallpaper.Scraper.ViewModels;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task>? executeAsync;
    private readonly Func<CancellationToken, Task>? executeAsyncWithToken;
    private readonly Func<bool>? canExecute;
    private bool isExecuting;

    public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        this.executeAsync = executeAsync;
        this.canExecute = canExecute;
    }

    public AsyncRelayCommand(Func<CancellationToken, Task> executeAsync, Func<bool>? canExecute = null)
    {
        executeAsyncWithToken = executeAsync;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !isExecuting && (canExecute?.Invoke() ?? true);

    public void Execute(object? parameter) => _ = ExecuteAsync(CancellationToken.None);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (isExecuting)
            return;

        isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            if (executeAsyncWithToken is not null)
                await executeAsyncWithToken(cancellationToken);
            else if (executeAsync is not null)
                await executeAsync();
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }
}

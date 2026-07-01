using System.Windows.Input;

namespace AStar.Dev.Wallpaper.Scrapper.ViewModels;

public sealed class AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null) : ICommand
{
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (canExecute?.Invoke() ?? true);

    public void Execute(object? parameter) => _ = ExecuteAsync();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private async Task ExecuteAsync()
    {
        if(_isExecuting)
            return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await executeAsync();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }
}

using System.Windows.Input;

namespace MacWinUI.App.ViewModels;

public sealed class AsyncRelayCommand<T>(
    Func<T, CancellationToken, Task> execute,
    Action<Exception> exceptionHandler,
    Predicate<T>? canExecute = null) : ICommand
{
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting
            && parameter is T value
            && (canExecute?.Invoke(value) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter) || parameter is not T value)
        {
            return;
        }

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await execute(value, CancellationToken.None);
        }
        catch (Exception exception)
        {
            exceptionHandler(exception);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    private void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

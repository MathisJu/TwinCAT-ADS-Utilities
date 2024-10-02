using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AdsUtilitiesUI;

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object, Task> _executeWithParameter;
    private readonly Func<Task> _executeWithoutParameter;
    private readonly Func<bool> _canExecute;
    private bool _isExecuting;

    public event EventHandler CanExecuteChanged;

    // Constructor for execution with parameter
    public AsyncRelayCommand(Func<object, Task> executeWithParameter, Func<bool> canExecute = null)
    {
        _executeWithParameter = executeWithParameter;
        _canExecute = canExecute;
    }

    // Constructor for execution without parameter
    public AsyncRelayCommand(Func<Task> executeWithoutParameter, Func<bool> canExecute = null)
    {
        _executeWithoutParameter = executeWithoutParameter;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return !_isExecuting && (_canExecute == null || _canExecute());
    }

    public async void Execute(object parameter)
    {
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            if (_executeWithParameter != null)
            {
                await _executeWithParameter(parameter);
            }
            else if (_executeWithoutParameter != null)
            {
                await _executeWithoutParameter();
            }
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
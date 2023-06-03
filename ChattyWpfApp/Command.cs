using System;
using System.Windows.Input;

namespace ChattyWpfApp
{
    public class Command : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public Command(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
                return true;

            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged;
    }
}
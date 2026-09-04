using System;
using System.Windows.Input;

namespace TwoD5DMonitoring.Designer
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }

    public class UndoableCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Action _undo;
        private readonly Func<bool> _canExecute;

        public UndoableCommand(Action execute, Action undo, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        
        public void Execute(object parameter) => _execute();
        
        public void Undo() => _undo();
    }
}
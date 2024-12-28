using System.Windows.Input;

namespace Nameless.InfoPhoenix.Application;

public sealed class EmptyCommand : ICommand {
    public static ICommand Instance { get; } = new EmptyCommand();

    static EmptyCommand() { }

    private EmptyCommand() { }

    public bool CanExecute(object? parameter)
        => false;

    public void Execute(object? parameter) { }

    public event EventHandler? CanExecuteChanged {
        add { }
        remove { }
    }
}
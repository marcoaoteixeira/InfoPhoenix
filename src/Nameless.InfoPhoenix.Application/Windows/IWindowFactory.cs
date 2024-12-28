using System.Diagnostics.CodeAnalysis;

namespace Nameless.InfoPhoenix.Application.Windows;

public interface IWindowFactory {
    bool TryCreate<TWindow>(object? owner, [NotNullWhen(returnValue: true)] out TWindow? window)
        where TWindow : IWindow;
}
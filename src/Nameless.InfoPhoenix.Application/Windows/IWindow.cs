using Nameless.InfoPhoenix.Dialogs;

namespace Nameless.InfoPhoenix.Application.Windows;

public interface IWindow {
    void SetTitle(string title);
    void SetOwner(object? owner);
    DialogResult ShowDialog(StartupLocation startupLocation);
    void Show(StartupLocation startupLocation);
    void Close();
}
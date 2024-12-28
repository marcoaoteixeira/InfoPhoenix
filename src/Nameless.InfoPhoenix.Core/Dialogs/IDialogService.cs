namespace Nameless.InfoPhoenix.Dialogs;

public interface IDialogService {
    DialogResult ShowDialog(string title,
                            string message,
                            DialogButtons buttons,
                            DialogIcon icon,
                            object? owner);
}
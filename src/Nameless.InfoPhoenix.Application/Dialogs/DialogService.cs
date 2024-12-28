using Nameless.InfoPhoenix.Dialogs;

namespace Nameless.InfoPhoenix.Application.Dialogs;

public sealed class DialogService : IDialogService {
    public DialogResult ShowDialog(string title,
                                   string message,
                                   DialogButtons buttons,
                                   DialogIcon icon,
                                   object? owner) {
        var dialogWindow = new DialogWindow();

        dialogWindow.SetTitle(title);
        dialogWindow.SetMessage(message);
        dialogWindow.SetButtons(buttons);
        dialogWindow.SetIcon(icon);
        dialogWindow.SetOwner(owner);

        return dialogWindow.ShowDialogBox();
    }
}
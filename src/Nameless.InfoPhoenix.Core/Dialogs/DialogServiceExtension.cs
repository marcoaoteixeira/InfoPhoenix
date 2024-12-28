namespace Nameless.InfoPhoenix.Dialogs;

public static class DialogServiceExtension {
    public static DialogResult ShowError(this IDialogService self, string title, string message, object? owner = null)
        => self.ShowDialog(title, message, DialogButtons.Confirm, DialogIcon.Error, owner: owner);

    public static DialogResult ShowWarning(this IDialogService self, string title, string message, object? owner = null)
        => self.ShowDialog(title, message, DialogButtons.Confirm, DialogIcon.Warning, owner: owner);

    public static DialogResult ShowAttention(this IDialogService self, string title, string message, object? owner = null)
        => self.ShowDialog(title, message, DialogButtons.Confirm, DialogIcon.Attention, owner: owner);

    public static DialogResult ShowInformation(this IDialogService self, string title, string message, object? owner = null)
        => self.ShowDialog(title, message, DialogButtons.Confirm, DialogIcon.Information, owner: owner);

    public static DialogResult ShowQuestion(this IDialogService self, string title, string message, DialogButtons buttons, object? owner = null)
        => self.ShowDialog(title, message, buttons, DialogIcon.Question, owner: owner);
}
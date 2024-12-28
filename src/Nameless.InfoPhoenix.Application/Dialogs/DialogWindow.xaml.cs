using System.Windows;
using System.Windows.Controls;
using Nameless.InfoPhoenix.Dialogs;

namespace Nameless.InfoPhoenix.Application.Dialogs;

public partial class DialogWindow {
    private DialogResult _dialogResult = InfoPhoenix.Dialogs.DialogResult.Confirm;

    public DialogWindow() {
        InitializeComponent();
    }

    public void SetTitle(string title)
        => TitleTextBlock.Text = title;

    public void SetMessage(string message)
        => MessageTextBlock.Text = message;

    public void SetButtons(DialogButtons buttons) {
        ConfirmButton.Visibility = buttons is DialogButtons.Confirm or DialogButtons.ConfirmCancel
            ? Visibility.Visible
            : Visibility.Collapsed;

        YesButton.Visibility = buttons is DialogButtons.YesNo or DialogButtons.YesNoCancel
            ? Visibility.Visible
            : Visibility.Collapsed;

        NoButton.Visibility = buttons is DialogButtons.YesNo or DialogButtons.YesNoCancel
            ? Visibility.Visible
            : Visibility.Collapsed;

        CancelButton.Visibility = buttons is DialogButtons.ConfirmCancel or DialogButtons.YesNoCancel
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void SetIcon(DialogIcon icon) {
        IconSymbolIcon.Symbol = icon.ToSymbolRegular();
        IconSymbolIcon.Foreground = icon.ToBrush();
    }

    public void SetOwner(object? owner)
        => Owner = owner as Window ?? System.Windows.Application.Current.MainWindow;

    public DialogResult ShowDialogBox() {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        ShowDialog();

        return _dialogResult;
    }

    private void ButtonHandler(object sender, RoutedEventArgs _) {
        if (sender is Button { Tag: DialogResult dialogResult }) {
            _dialogResult = dialogResult;
        }

        Close();
    }
}

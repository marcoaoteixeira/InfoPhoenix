using Nameless.InfoPhoenix.Application.Notification;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Application;

public static class SnackbarServiceExtension {
    public static void Show(this ISnackbarService self, SnackbarParameters parameters)
        => self.Show(parameters.Title ?? parameters.Appearance.GetTitle(),
                     parameters.Content,
                     parameters.Appearance,
                     parameters.Appearance.GetIcon(),
                     Constants.Presentation.SnackbarTimeout);
}
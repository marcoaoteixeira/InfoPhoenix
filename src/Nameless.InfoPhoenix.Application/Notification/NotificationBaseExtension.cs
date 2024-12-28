using Nameless.InfoPhoenix.Notification;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Application.Notification;

public static class NotificationBaseExtension {
    public static SnackbarParameters ToSnackbarParameters(this NotificationBase self)
        => new() {
            Title = self.Title,
            Content = self.Message,
            Appearance = self.Type switch {
                NotificationType.Error => ControlAppearance.Danger,
                NotificationType.Success => ControlAppearance.Success,
                NotificationType.Warning => ControlAppearance.Caution,
                _ => ControlAppearance.Info
            }
        };
}
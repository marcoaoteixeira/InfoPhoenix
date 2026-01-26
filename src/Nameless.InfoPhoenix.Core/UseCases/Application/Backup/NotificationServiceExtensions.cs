using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Application.Backup;

internal static class NotificationServiceExtensions {
    extension(INotificationService self) {
        internal Task NotifyApplicationBackupStartingAsync() {
            return self.PublishAsync(new PerformApplicationBackupNotification(
                message: Strings.PerformApplicationBackupNotification_Starting,
                type: NotificationType.Information
            ));
        }

        internal Task NotifyApplicationBackupFailureAsync() {
            return self.PublishAsync(new PerformApplicationBackupNotification(
                message: Strings.PerformApplicationBackupNotification_Failure,
                type: NotificationType.Error
            ));
        }

        internal Task NotifyApplicationBackupCleanUpAsync() {
            return self.PublishAsync(new PerformApplicationBackupNotification(
                message: Strings.PerformApplicationBackupNotification_CleanUp,
                type: NotificationType.Information
            ));
        }

        internal Task NotifyApplicationBackupSuccessAsync(string backupFilePath) {
            return self.PublishAsync(new PerformApplicationBackupNotification(
                message: string.Format(Strings.PerformApplicationBackupNotification_Success, backupFilePath),
                type: NotificationType.Success
            ));
        }
    }
}
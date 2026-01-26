using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

internal static class NotificationServiceExtensions {
    extension(INotificationService self) {
        internal Task NotifyStartingAsync() {
            return self.PublishAsync(new PerformDatabaseBackupNotification(
                message: Strings.PerformDatabaseBackupNotification_Starting,
                type: NotificationType.Information
            ));
        }

        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new PerformDatabaseBackupNotification(
                message: string.Format(Strings.PerformDatabaseBackupNotification_Failure, error),
                type: NotificationType.Error
            ));
        }

        internal Task NotifySuccessAsync(string backupFilePath) {
            return self.PublishAsync(new PerformDatabaseBackupNotification(
                message: string.Format(Strings.PerformDatabaseBackupNotification_Success, backupFilePath),
                type: NotificationType.Success
            ));
        }
    }
}

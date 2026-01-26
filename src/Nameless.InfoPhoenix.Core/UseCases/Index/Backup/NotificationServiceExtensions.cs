using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

internal static class NotificationServiceExtensions {
    extension(INotificationService self) {
        internal Task NotifyStartingAsync() {
            return self.PublishAsync(new PerformDocumentIndexBackupNotification(
                message: Strings.PerformDocumentIndexBackupNotification_Starting,
                type: NotificationType.Information
            ));
        }

        internal Task NotifySuccessAsync(string backupFilePath) {
            return self.PublishAsync(new PerformDocumentIndexBackupNotification(
                message: string.Format(Strings.PerformDocumentIndexBackupNotification_Success, backupFilePath),
                type: NotificationType.Success
            ));
        }

        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new PerformDocumentIndexBackupNotification(
                message: string.Format(Strings.PerformDocumentIndexBackupNotification_Failure, error),
                type: NotificationType.Error
            ));
        }
    }
}

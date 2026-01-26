using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

public record PerformDatabaseBackupNotification : INotification {
    public string Title => Strings.PerformDatabaseBackupNotification_Title;

    public string Message { get; }

    public NotificationType Type { get; }

    public PerformDatabaseBackupNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}

using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public record PerformDocumentIndexBackupNotification : INotification {
    public string Title => Strings.PerformDocumentIndexBackupNotification_Title;

    public string Message { get; }

    public NotificationType Type { get; }

    public PerformDocumentIndexBackupNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}

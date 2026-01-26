using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Application.Backup;

public record PerformApplicationBackupNotification : INotification {
    public string Title => Strings.PerformApplicationBackupNotification_Title;

    public string Message { get; }

    public NotificationType Type { get; }

    public PerformApplicationBackupNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
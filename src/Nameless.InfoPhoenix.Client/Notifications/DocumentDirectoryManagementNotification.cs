using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.Client.Notifications;

public record DocumentDirectoryManagementNotification : INotification {
    public string Title => "Gerir Diretórios de Documentos";
    public string Message { get; }
    public NotificationType Type { get; }

    private DocumentDirectoryManagementNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }

    public static DocumentDirectoryManagementNotification DoubleClick(string message) {
        return new DocumentDirectoryManagementNotification(message, NotificationType.Information);
    }
}

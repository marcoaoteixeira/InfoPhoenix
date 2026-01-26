using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Search;

public record SearchDocumentsNotification : INotification {
    public string Title => "Buscar de Documentos";

    public string Message { get; }
    
    public NotificationType Type { get; }

    public SearchDocumentsNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
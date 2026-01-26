using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

public record DeleteDocumentDirectoryNotification : INotification {
    public string Title => "Remover Diretório de Documentos";
    
    public string Message { get; }
    
    public NotificationType Type { get; }

    public DeleteDocumentDirectoryNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
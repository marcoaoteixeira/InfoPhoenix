using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

public record UpdateDocumentDirectoryNotification : INotification {
    public string Title => "Atualizar Diretório de Documentos";
    
    public string Message { get; }
    
    public NotificationType Type { get; }

    public UpdateDocumentDirectoryNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
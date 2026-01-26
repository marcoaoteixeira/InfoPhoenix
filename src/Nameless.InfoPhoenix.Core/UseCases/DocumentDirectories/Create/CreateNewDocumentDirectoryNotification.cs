using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

public record CreateNewDocumentDirectoryNotification : INotification {
    public string Title => "Criar Novo Diretório de Documentos";

    public string Message { get; }
    
    public NotificationType Type { get; }

    public CreateNewDocumentDirectoryNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
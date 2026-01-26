using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

public record FetchDocumentsForDocumentDirectoryNotification : INotification {
    public string Title => "Buscar Documentos em Diretório de Documentos";
    
    public string Message { get; }
    
    public NotificationType Type { get; }

    public FetchDocumentsForDocumentDirectoryNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
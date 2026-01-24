using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;

public record IndexDocumentsNotification : INotification {
    public string Title => "Indexar Documentos do Diretório de Documentos";

    public string Message { get; }

    public NotificationType Type { get; }

    public IndexDocumentsNotification(string message, NotificationType type) {
        Message = message;
        Type = type;
    }
}
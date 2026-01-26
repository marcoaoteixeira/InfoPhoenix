using System.Text;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;

internal static class NotificationExtensions {
    extension(INotificationService self) {
        internal Task NotifyStartingAsync() {
            return self.PublishAsync(new IndexDocumentsNotification(
                message: "Iniciando indexação de documentos...",
                type: NotificationType.Information
            ));
        }

        internal Task NotifyFinishAsync() {
            return self.PublishAsync(new IndexDocumentsNotification(
                message: "Indexação de documentos concluída com sucesso.",
                type: NotificationType.Success
            ));
        }

        internal Task NotifyMissingDocumentDirectoryAsync() {
            return self.PublishAsync(new IndexDocumentsNotification(
                message: "Não foi possível localizar o diretório de documentos especificado.",
                type: NotificationType.Warning
            ));
        }

        internal Task NotifyDocumentsToIndexAsync(int totalDocuments) {
            return self.PublishAsync(new IndexDocumentsNotification(
                message: $"Total de documentos para indexação: {totalDocuments}",
                type: NotificationType.Information
            ));
        }

        internal Task NotifyIndexingAsync(Document[] documents) {
            var sb = new StringBuilder();
            sb.AppendLine("Indexando documentos:");
            foreach (var document in documents) {
                sb.AppendLine($"\t- {document.FilePath}");
            }
            sb.AppendLine();

            return self.PublishAsync(new IndexDocumentsNotification(
                message: sb.ToString(),
                type: NotificationType.Information
            ));
        }

        internal Task NotifyCancellationAsync() {
            return self.PublishAsync(new IndexDocumentsNotification(
                message: "Indexação foi cancelada pelo usuário.",
                type: NotificationType.Warning
            ));
        }
    }
}

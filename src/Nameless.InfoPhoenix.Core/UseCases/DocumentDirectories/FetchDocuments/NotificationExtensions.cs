using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

internal static class NotificationExtensions {
    extension(INotificationService self) {
        internal Task NotifySuccessAsync(string label, int documentCount) {
            return self.PublishAsync(new FetchDocumentsForDocumentDirectoryNotification(
                message: $"Busca de documentos para o diretório de documentos '{label}' concluída com sucesso. Total de documentos: {documentCount}",
                type: NotificationType.Success
            ));
        }

        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new FetchDocumentsForDocumentDirectoryNotification(
                message: $"Ocorreu um erro ao buscar documentos para o diretório de documentos: {error}",
                type: NotificationType.Error
            ));
        }

        internal Task NotifyAddNewDocumentSuccessAsync(string filePath) {
            return self.PublishAsync(new FetchDocumentsForDocumentDirectoryNotification(
                message: $"Novo documento adicionado: '{filePath}'",
                type: NotificationType.Information
            ));
        }

        internal Task NotifyDocumentUpdateSuccessAsync(string filePath) {
            return self.PublishAsync(new FetchDocumentsForDocumentDirectoryNotification(
                message: $"Documento '{filePath}' foi atualizado.",
                type: NotificationType.Information
            ));
        }

        internal Task NotifyReadingDocumentContentAsync(string filePath) {
            return self.PublishAsync(new FetchDocumentsForDocumentDirectoryNotification(
                message: $"Lendo conteúdo do documento '{filePath}'...",
                type: NotificationType.Information
            ));
        }

        internal Task NotifyReadDocumentContentFailureAsync(string filePath, string error) {
            return self.PublishAsync(new FetchDocumentsForDocumentDirectoryNotification(
                message: $"Ocorreu um erro ao ler o conteúdo do documento '{filePath}'. Erro: {error}",
                type: NotificationType.Error
            ));
        }
    }
}

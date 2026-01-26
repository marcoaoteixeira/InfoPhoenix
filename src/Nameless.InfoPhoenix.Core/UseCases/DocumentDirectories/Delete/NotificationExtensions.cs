using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

internal static class NotificationExtensions {
    extension(INotificationService self) {
        internal Task NotifySuccessAsync(string label) {
            return self.PublishAsync(new DeleteDocumentDirectoryNotification(
                message: $"Diretório de Documentos '{label}' removido com sucesso.",
                type: NotificationType.Success
            ));
        }

        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new DeleteDocumentDirectoryNotification(
                message: $"Ocorreu um erro ao remover o diretório de documentos: {error}",
                type: NotificationType.Error
            ));
        }
    }
}

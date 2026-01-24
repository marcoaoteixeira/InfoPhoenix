using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

internal static class NotificationExtensions {
    extension(INotificationService self) {
        internal Task NotifySuccessAsync(string label) {
            return self.PublishAsync(new UpdateDocumentDirectoryNotification(
                message: $"Diretório de Documentos '{label}' atualizado com sucesso.",
                type: NotificationType.Success
            ));
        }

        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new UpdateDocumentDirectoryNotification(
                message: $"Ocorreu um erro ao atualizar o diretório de documentos: {error}",
                type: NotificationType.Error
            ));
        }
    }
}

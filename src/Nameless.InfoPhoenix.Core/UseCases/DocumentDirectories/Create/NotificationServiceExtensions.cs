using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

internal static class NotificationServiceExtensions {
    extension(INotificationService self) {
        internal Task NotifySuccessAsync(string label) {
            return self.PublishAsync(new CreateNewDocumentDirectoryNotification(
                message: $"Diretório de Documentos '{label}' criado com sucesso.",
                type: NotificationType.Success
            ));
        }

        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new CreateNewDocumentDirectoryNotification(
                message: $"Ocorreu um erro ao criar o diretório de documentos: {error}",
                type: NotificationType.Error
            ));
        }
    }
}

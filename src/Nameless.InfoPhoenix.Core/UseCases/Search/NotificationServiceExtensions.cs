using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Search;

internal static class NotificationServiceExtensions {
    extension(INotificationService self) {
        internal Task NotifyFailureAsync(string error) {
            return self.PublishAsync(new SearchDocumentsNotification(
                message: $"Ocorreu um erro durante a busca de documentos. Erro: {error}",
                type: NotificationType.Error
            ));
        }
    }
}

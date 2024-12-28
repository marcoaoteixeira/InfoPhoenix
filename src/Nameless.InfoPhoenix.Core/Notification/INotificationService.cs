namespace Nameless.InfoPhoenix.Notification {
    public interface INotificationService {
        void Subscribe<TNotification>(object recipient, Action<object, TNotification> handler)
            where TNotification : NotificationBase;

        void Unsubscribe<TNotification>(object recipient)
            where TNotification : NotificationBase;

        Task PublishAsync<TNotification>(TNotification notification)
            where TNotification : NotificationBase;
    }
}

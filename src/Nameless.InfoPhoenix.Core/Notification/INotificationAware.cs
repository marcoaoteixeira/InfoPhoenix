namespace Nameless.InfoPhoenix.Notification;

public interface INotificationAware {
    void SubscribeForNotifications();
    void UnsubscribeFromNotifications();
}
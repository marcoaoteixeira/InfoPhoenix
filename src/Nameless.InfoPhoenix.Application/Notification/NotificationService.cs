using CommunityToolkit.Mvvm.Messaging;
using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Application.Notification;

public sealed class NotificationService : INotificationService {
    private readonly IMessenger _messenger;

    public NotificationService(IMessenger messenger) {
        _messenger = Prevent.Argument.Null(messenger);
    }

    public void Subscribe<TNotification>(object recipient, Action<object, TNotification> handler)
        where TNotification : NotificationBase
        => _messenger.Register<TNotification>(recipient,
                                              handler: (sender, notification) => handler(sender, notification));

    public void Unsubscribe<TNotification>(object recipient)
        where TNotification : NotificationBase
        => _messenger.Unregister<TNotification>(recipient);

    public Task PublishAsync<TNotification>(TNotification notification)
        where TNotification : NotificationBase {
        _messenger.Send(notification);

        return Task.CompletedTask;
    }
}
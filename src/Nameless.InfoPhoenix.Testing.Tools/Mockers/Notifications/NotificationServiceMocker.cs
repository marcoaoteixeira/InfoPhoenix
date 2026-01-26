using Moq;
using Nameless.Testing.Tools.Mockers;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.Testing.Tools.Mockers.Notifications;

public class NotificationServiceMocker : Mocker<INotificationService> {
    public NotificationServiceMocker WithPublishAsync<TNotification>(Action<TNotification>? callback = null)
        where TNotification : class, INotification {
        var flow = MockInstance.Setup(mock => mock.PublishAsync(It.IsAny<TNotification>()))
                               .Returns(Task.CompletedTask);

        if (callback is not null) {
            flow.Callback(callback);
        }

        return this;
    }

    public void VerifyPublishAsync<TNotification>(int times = 1)
        where TNotification : class, INotification {
        MockInstance.Verify(mock => mock.PublishAsync(It.IsAny<TNotification>()), Times.Exactly(times));
    }
}

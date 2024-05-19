using Nameless.InfoPhoenix.Objects;

namespace Nameless.InfoPhoenix.Infrastructure {
    public interface IPubSubService {
        #region Methods

        void Subscribe<TNotification>(object recipient, Action<object, TNotification> handler)
            where TNotification : Notification;

        void Unsubscribe<TNotification>(object recipient)
            where TNotification : Notification;

        Task PublishAsync<TNotification>(TNotification notification)
            where TNotification : Notification;

        #endregion
    }
}

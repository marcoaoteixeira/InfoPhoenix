using CommunityToolkit.Mvvm.Messaging;
using Nameless.InfoPhoenix.Objects;

namespace Nameless.InfoPhoenix.Infrastructure.Impl {
    public sealed class PubSubService : IPubSubService {
        #region Private Read-Only Fields

        private readonly IMessenger _messenger;

        #endregion

        #region Public Constructors

        public PubSubService(IMessenger messenger) {
            _messenger = Guard.Against.Null(messenger, nameof(messenger));
        }

        #endregion

        #region IPubSubService Members

        public void Subscribe<TNotification>(object recipient, Action<object, TNotification> handler)
            where TNotification : Notification
            => _messenger.Register(recipient, (object handlerRecipient, TNotification handlerNotification) => handler(handlerRecipient, handlerNotification));

        public void Unsubscribe<TNotification>(object recipient)
            where TNotification : Notification
            => _messenger.Unregister<TNotification>(recipient);

        public Task PublishAsync<TNotification>(TNotification notification)
            where TNotification : Notification
            => Task.Run(() => _messenger.Send(notification));

        #endregion
    }
}

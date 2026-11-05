using Nameless.Windows.Events;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IEventPublisher"/> fake. Records every published event so a test can
/// assert what was published (and, for the <c>Publish*Async</c> notification helpers built on
/// top of this interface, what title/message/type it carried) without wiring the real
/// in-process messenger.
/// </summary>
internal sealed class FakeEventPublisher : IEventPublisher {
    public List<Event> PublishedEvents { get; } = [];

    /// <summary>
    /// Every active registration, in the order <see cref="Register{TEvent}"/> was called.
    /// <see cref="Unregister{TEvent}"/> removes the matching entry. Needed now that
    /// <c>MainWindow</c> registers for <c>SnackBarNotificationEvent</c> (plan 2.5/M1).
    /// </summary>
    public List<(Type EventType, object Subscriber, Delegate Handler)> Registrations { get; } = [];

    /// <summary>
    /// When set, every <see cref="PublishAsync{TEvent}"/> throws this instead of recording the
    /// event - simulates a failing notification (plan 2.3 step 5/B14), which must not break the
    /// caller that already applied/saved its change.
    /// </summary>
    public Exception? ThrowOnPublish { get; set; }

    /// <summary>
    /// Runs before an event is recorded, letting a test react to a specific published event -
    /// for instance, cancelling the token the handler is running under exactly when a given
    /// step's Information entry is published (IPX-0009/B8), without needing real concurrency.
    /// </summary>
    public Action<Event>? OnPublish { get; set; }

    public void Register<TEvent>(object subscriber, Action<object, TEvent> handler) where TEvent : Event {
        Registrations.Add((typeof(TEvent), subscriber, handler));
    }

    public void Unregister<TEvent>(object subscriber) where TEvent : Event {
        Registrations.RemoveAll(r => r.EventType == typeof(TEvent) && ReferenceEquals(r.Subscriber, subscriber));
    }

    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : Event {
        OnPublish?.Invoke(@event);

        if (ThrowOnPublish is not null) {
            throw ThrowOnPublish;
        }

        PublishedEvents.Add(@event);

        return Task.CompletedTask;
    }
}
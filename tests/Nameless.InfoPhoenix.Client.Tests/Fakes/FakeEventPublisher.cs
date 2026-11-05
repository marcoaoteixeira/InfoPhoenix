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

    public void Register<TEvent>(object subscriber, Action<object, TEvent> handler) where TEvent : Event {
        throw new NotSupportedException();
    }

    public void Unregister<TEvent>(object subscriber) where TEvent : Event {
        throw new NotSupportedException();
    }

    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : Event {
        PublishedEvents.Add(@event);

        return Task.CompletedTask;
    }
}

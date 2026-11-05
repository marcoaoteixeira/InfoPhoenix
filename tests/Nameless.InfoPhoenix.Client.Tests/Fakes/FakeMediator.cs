using Nameless.Mediator;
using Nameless.Mediator.Events;
using Nameless.Mediator.Requests;
using Nameless.Mediator.Streams;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IMediator"/> fake. Records every request executed through
/// <see cref="ExecuteAsync(IRequest, CancellationToken)"/>; the other members are not used by
/// this ticket's code and throw if ever touched.
/// </summary>
internal sealed class FakeMediator : IMediator {
    public List<IRequest> ExecutedRequests { get; } = [];

    public Func<IRequest, CancellationToken, Task> OnExecuteAsync { get; set; } = (_, _) => Task.CompletedTask;

    public Task ExecuteAsync(IRequest request, CancellationToken cancellationToken) {
        ExecutedRequests.Add(request);

        return OnExecuteAsync(request, cancellationToken);
    }

    public Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken) {
        throw new NotSupportedException();
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IEvent {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<TResponse> CreateAsync<TResponse>(IStream<TResponse> stream, CancellationToken cancellationToken) {
        throw new NotSupportedException();
    }
}

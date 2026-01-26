using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.Search;

public record SearchDocumentsRequest : IRequest<SearchDocumentsResponse> {
    public Guid[] DocumentDirectories { get; init; } = [];

    public required string Term { get; init; }

    public int Start { get; init; }

    public int Limit { get; init; } = 10_000;
}
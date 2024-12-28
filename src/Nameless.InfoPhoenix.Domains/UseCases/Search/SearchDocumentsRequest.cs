using MediatR;

namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public sealed record SearchDocumentsRequest : IRequest<SearchDocumentsResponse> {
    public string QueryTerm { get; init; } = string.Empty;
}
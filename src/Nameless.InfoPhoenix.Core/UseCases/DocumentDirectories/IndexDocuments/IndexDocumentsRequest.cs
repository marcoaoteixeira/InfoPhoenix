using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;

public record IndexDocumentsRequest : IRequest<IndexDocumentsResponse> {
    public required Guid DocumentDirectoryID { get; init; }
}
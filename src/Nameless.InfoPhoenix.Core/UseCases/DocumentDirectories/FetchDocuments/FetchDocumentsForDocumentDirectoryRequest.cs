using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

public record FetchDocumentsForDocumentDirectoryRequest : IRequest<FetchDocumentsForDocumentDirectoryResponse> {
    public required Guid DocumentDirectoryID { get; init; }
}
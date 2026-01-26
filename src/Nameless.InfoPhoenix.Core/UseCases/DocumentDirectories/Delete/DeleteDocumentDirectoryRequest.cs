using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

public record DeleteDocumentDirectoryRequest : IRequest<DeleteDocumentDirectoryResponse> {
    public required Guid DocumentDirectoryID { get; init; }
}
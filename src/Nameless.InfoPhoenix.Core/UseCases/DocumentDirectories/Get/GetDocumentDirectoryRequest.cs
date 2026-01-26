using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Get;

public record GetDocumentDirectoryRequest : IRequest<GetDocumentDirectoryResponse> {
    public required Guid DocumentDirectoryID { get; init; }
}
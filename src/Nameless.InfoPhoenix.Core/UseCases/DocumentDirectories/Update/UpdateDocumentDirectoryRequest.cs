using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

public record UpdateDocumentDirectoryRequest : IRequest<UpdateDocumentDirectoryResponse> {
    public required Guid DocumentDirectoryID { get; init; }
    public required string Label { get; init; }
    public required string DirectoryPath { get; init; }
    public required int DisplayPosition { get; init; }
}
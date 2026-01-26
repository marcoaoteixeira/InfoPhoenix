using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

public record CreateNewDocumentDirectoryRequest : IRequest<CreateNewDocumentDirectoryResponse> {
    public required string Label { get; init; }
    public required string DirectoryPath { get; init; }
    public required int DisplayPosition { get; init; }
}
using MediatR;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Delete;

public sealed record DeleteDocumentDirectoryRequest : IRequest {
    public Guid ID { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DirectoryPath { get; init; } = string.Empty;
    public int Order { get; init; }
}
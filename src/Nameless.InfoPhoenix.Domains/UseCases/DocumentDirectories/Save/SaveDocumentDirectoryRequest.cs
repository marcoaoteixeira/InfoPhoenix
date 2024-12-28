using MediatR;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;

public sealed record SaveDocumentDirectoryRequest : IRequest {
    public Guid ID { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DirectoryPath { get; init; } = string.Empty;
    public int Order { get; init; }
}
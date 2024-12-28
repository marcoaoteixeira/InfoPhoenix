using MediatR;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Indexing;

public sealed record IndexDocumentDirectoryRequest : IRequest {
    public Guid DocumentDirectoryID { get; init; }

    public IProgress<string> Reporter { get; init; } = NullProgress<string>.Instance;
}
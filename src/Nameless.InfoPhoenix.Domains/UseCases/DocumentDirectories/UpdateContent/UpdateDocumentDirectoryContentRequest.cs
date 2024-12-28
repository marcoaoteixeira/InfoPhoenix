using MediatR;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.UpdateContent;

public sealed record UpdateDocumentDirectoryContentRequest : IRequest {
    public Guid DocumentDirectoryID { get; init; }

    public IProgress<string> Reporter { get; init; } = NullProgress<string>.Instance;
}
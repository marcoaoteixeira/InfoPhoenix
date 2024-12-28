using MediatR;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Get;

public sealed record GetDocumentDirectoryRequest : IRequest<DocumentDirectoryDto> {
    public Guid ID { get; init; }
}
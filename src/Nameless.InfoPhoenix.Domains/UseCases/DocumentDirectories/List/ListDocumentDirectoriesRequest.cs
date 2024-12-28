using MediatR;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.List;

public sealed record ListDocumentDirectoriesRequest : IRequest<DocumentDirectoryDto[]>;
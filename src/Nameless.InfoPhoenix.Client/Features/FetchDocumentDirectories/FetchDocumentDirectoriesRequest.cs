using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.Client.Features.FetchDocumentDirectories;

public class FetchDocumentDirectoriesRequest : IRequest<DocumentDirectoryDto[]>;
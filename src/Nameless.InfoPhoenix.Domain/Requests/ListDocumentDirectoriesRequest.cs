using MediatR;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Requests {
    public sealed record ListDocumentDirectoriesRequest : IRequest<DocumentDirectoryCollectionResponse> {
    }
}

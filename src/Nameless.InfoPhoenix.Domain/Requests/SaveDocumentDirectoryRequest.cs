using MediatR;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Requests {
    public sealed record SaveDocumentDirectoryRequest : IRequest<DocumentDirectoryResponse> {
        #region Public Properties

        public DocumentDirectoryDto DocumentDirectory { get; init; } = DocumentDirectoryDto.Empty;

        #endregion
    }
}

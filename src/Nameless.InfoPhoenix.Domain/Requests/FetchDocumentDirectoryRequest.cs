using MediatR;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Requests {
    public sealed record FetchDocumentDirectoryRequest : IRequest<DocumentDirectoryResponse> {
        #region Public Properties

        public Guid DocumentDirectoryID { get; set; }

        #endregion
    }
}

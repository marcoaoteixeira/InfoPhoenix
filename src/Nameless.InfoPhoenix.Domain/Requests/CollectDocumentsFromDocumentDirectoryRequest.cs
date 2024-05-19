using MediatR;
using Nameless.InfoPhoenix.Responses;

namespace Nameless.InfoPhoenix.Domain.Requests {
    public sealed record CollectDocumentsFromDocumentDirectoryRequest : IRequest<EmptyResponse> {
        #region Public Properties

        public Guid DocumentDirectoryID { get; init; }

        #endregion
    }
}

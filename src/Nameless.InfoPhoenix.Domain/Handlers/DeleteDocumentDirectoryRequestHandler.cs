using MediatR;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Responses;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class DeleteDocumentDirectoryRequestHandler : IRequestHandler<DeleteDocumentDirectoryRequest, EmptyResponse> {
        #region Private Read-Only Fields

        private readonly AppDbContext _appDbContext;

        #endregion

        #region Public Constructors

        public DeleteDocumentDirectoryRequestHandler(AppDbContext appDbContext) {
            _appDbContext = Guard.Against.Null(appDbContext, nameof(appDbContext));
        }

        #endregion

        #region IRequestHandler<DeleteDocumentDirectoryRequest, EmptyResponse> Members

        public async Task<EmptyResponse> Handle(DeleteDocumentDirectoryRequest request, CancellationToken cancellationToken) {
            var current = await _appDbContext
                                .DocumentDirectories
                                .Where(entity => entity.ID == request.DocumentDirectoryID)
                                .ExecuteDeleteAsync(cancellationToken);

            var response = new EmptyResponse {
                Error = current != 1
                    ? Root.DbErrors.DOCUMENT_DIRECTORY_NOT_DELETED
                    : null
            };

            return response;
        } 

        #endregion
    }
}

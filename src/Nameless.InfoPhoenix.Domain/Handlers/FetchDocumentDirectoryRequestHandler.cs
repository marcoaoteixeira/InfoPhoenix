using MediatR;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class FetchDocumentDirectoryRequestHandler : IRequestHandler<FetchDocumentDirectoryRequest, DocumentDirectoryResponse> {
        #region Private Read-Only Fields

        private readonly AppDbContext _appDbContext;

        #endregion

        #region Public Constructors

        public FetchDocumentDirectoryRequestHandler(AppDbContext appDbContext) {
            _appDbContext = Guard.Against.Null(appDbContext, nameof(appDbContext));
        }

        #endregion

        #region IRequestHandler<FetchDocumentDirectoryRequest, DocumentDirectoryResponse> Members

        public async Task<DocumentDirectoryResponse> Handle(FetchDocumentDirectoryRequest request, CancellationToken cancellationToken) {
            var current = await _appDbContext
                                .DocumentDirectories
                                .AsNoTracking()
                                .FirstOrDefaultAsync(documentDirectory => documentDirectory.ID == request.DocumentDirectoryID,
                                                     cancellationToken);

            var response = new DocumentDirectoryResponse {
                Value = current.ToDto(),

                Error = current is null
                    ? Root.DbErrors.DOCUMENT_DIRECTORY_NOT_FOUND
                    : null
            };

            return response;
        }

        #endregion
    }
}

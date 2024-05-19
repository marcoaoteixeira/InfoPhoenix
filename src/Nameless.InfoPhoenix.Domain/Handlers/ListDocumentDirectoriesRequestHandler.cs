using MediatR;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class ListDocumentDirectoriesRequestHandler : IRequestHandler<ListDocumentDirectoriesRequest, DocumentDirectoryCollectionResponse> {
        #region Private Read-Only Fields

        private readonly AppDbContext _appDbContext;

        #endregion

        #region Public Constructors

        public ListDocumentDirectoriesRequestHandler(AppDbContext appDbContext) {
            _appDbContext = Guard.Against.Null(appDbContext, nameof(appDbContext));
        }

        #endregion

        #region IRequestHandler<ListDocumentDirectoriesRequest, DocumentDirectoryCollectionResponse> Members

        public async Task<DocumentDirectoryCollectionResponse> Handle(ListDocumentDirectoriesRequest request, CancellationToken cancellationToken) {
            var dtos = _appDbContext.DocumentDirectories
                                        .AsNoTracking()
                                        .OrderBy(entity => entity.Order)
                                        .Select(entity => entity.ToDto())
                                        .ToArray();

            foreach (var dto in dtos) {
                dto.DocumentCount = await _appDbContext
                                          .Documents
                                          .AsNoTracking()
                                          .Where(document => document.DocumentDirectoryID == dto.ID)
                                          .CountAsync(cancellationToken: cancellationToken);
            }

            var response = new DocumentDirectoryCollectionResponse {
                Value = dtos
            };

            return response;
        }

        #endregion
    }
}

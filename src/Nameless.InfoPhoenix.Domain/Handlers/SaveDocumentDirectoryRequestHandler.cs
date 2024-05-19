using MediatR;
using Nameless.InfoPhoenix.Domain.Entities;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class SaveDocumentDirectoryRequestHandler : IRequestHandler<SaveDocumentDirectoryRequest, DocumentDirectoryResponse> {
        #region Private Read-Only Fields

        private readonly AppDbContext _appDbContext;

        #endregion

        #region Public Constructors

        public SaveDocumentDirectoryRequestHandler(AppDbContext appDbContext) {
            _appDbContext = Guard.Against.Null(appDbContext, nameof(appDbContext));
        }

        #endregion

        #region IRequestHandler<SaveDocumentDirectoryRequest, DocumentDirectoryResponse> Members

        public async Task<DocumentDirectoryResponse> Handle(SaveDocumentDirectoryRequest request, CancellationToken cancellationToken) {
            var entity = await _appDbContext
                               .DocumentDirectories
                               .FindAsync([request.DocumentDirectory.ID], cancellationToken);

            if (entity is null) {
                entity = new DocumentDirectory {
                    ID = Guid.NewGuid(),
                    CreatedAt = DateTime.Now
                };

                await _appDbContext
                      .DocumentDirectories
                      .AddAsync(entity, cancellationToken);
            }
            else { entity.ModifiedAt = DateTime.Now; }

            entity.Label = request.DocumentDirectory.Label;
            entity.DirectoryPath = request.DocumentDirectory.DirectoryPath;
            entity.Order = request.DocumentDirectory.Order;
            entity.LastIndexingTime = request.DocumentDirectory.LastIndexingTime;

            var result = await _appDbContext.SaveChangesAsync(cancellationToken);

            var response = new DocumentDirectoryResponse {
                Value = entity.ToDto(),
                Error = result == 0 ? Root.DbErrors.DOCUMENT_DIRECTORY_NOT_SAVED : null
            };

            return response;
        }

        #endregion
    }
}

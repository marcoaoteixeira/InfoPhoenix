using MediatR;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Responses;
using Nameless.Lucene;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class IndexDocumentDirectoryRequestHandler : IRequestHandler<IndexDocumentDirectoryRequest, EmptyResponse> {
        #region Private Read-Only Fields

        private readonly AppDbContext _appDbContext;
        private readonly IIndexManager _indexManager;

        #endregion

        #region Public Constructors

        public IndexDocumentDirectoryRequestHandler(AppDbContext appDbContext, IIndexManager indexManager) {
            _appDbContext = Guard.Against.Null(appDbContext, nameof(appDbContext));
            _indexManager = Guard.Against.Null(indexManager, nameof(indexManager));
        }

        #endregion

        #region IRequestHandler<IndexDocumentDirectoryRequest, EmptyResponse> Members

        public async Task<EmptyResponse> Handle(IndexDocumentDirectoryRequest request, CancellationToken cancellationToken) {
            var documentDirectory = await _appDbContext
                                    .DocumentDirectories
                                    .FindAsync([request.DocumentDirectoryID], cancellationToken);

            if (documentDirectory is null) {
                return new EmptyResponse {
                    Error = Root.DbErrors.DOCUMENT_DIRECTORY_NOT_FOUND
                };
            }

            var documents = _appDbContext
                .Documents
                .Where(document => document.DocumentDirectoryID == request.DocumentDirectoryID &&
                                   document.LastIndexingTime == null);

            if (!documents.Any()) {
                return new EmptyResponse {
                    Error = Root.DbErrors.NO_DOCUMENTS_TO_INDEX
                };
            }

            documentDirectory.LastIndexingTime = DateTime.Now;
            documentDirectory.ModifiedAt = DateTime.Now;

            var index = _indexManager.GetOrCreate(Root.Index.NAME);
            var indexDocumentList = new List<IDocument>();
            foreach (var document in documents) {
                try { cancellationToken.ThrowIfCancellationRequested(); }
                catch (OperationCanceledException) {
                    return new EmptyResponse {
                        Error = Root.Index.INDEX_OPERATION_CANCELLED
                    };
                }

                document.LastIndexingTime = documentDirectory.LastIndexingTime;
                document.ModifiedAt = documentDirectory.ModifiedAt;

                indexDocumentList.Add(document.ToIndexDocument(index, documentDirectory));
            }
            index.StoreDocuments([.. indexDocumentList]);

            await _appDbContext.SaveChangesAsync(cancellationToken);

            return new EmptyResponse();
        }

        #endregion
    }
}

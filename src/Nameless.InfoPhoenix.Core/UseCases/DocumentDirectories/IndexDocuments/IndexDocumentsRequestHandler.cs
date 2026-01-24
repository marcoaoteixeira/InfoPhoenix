using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Entities;
using Nameless.InfoPhoenix.Extensions;
using Nameless.Lucene;
using Nameless.Lucene.Requests;
using Nameless.ObjectModel;
using Nameless.Results;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;

public class IndexDocumentsRequestHandler : RequestHandlerBase<IndexDocumentsRequest, IndexDocumentsResponse> {
    private const int INDEX_CHUNK_SIZE = 16;
    
    private IIndex CurrentIndex { get; }

    private TimeProvider TimeProvider { get; }

    public IndexDocumentsRequestHandler(
        AppDbContext appDbContext,
        INotificationService notificationService,
        IValidationService validationService,
        IIndexProvider indexProvider,
        TimeProvider timeProvider,
        ILogger<IndexDocumentsRequestHandler> logger
    ) : base(
        appDbContext,
        notificationService,
        validationService,
        logger
    ) {
        CurrentIndex = indexProvider.Get(Constants.Index.Name);
        TimeProvider = timeProvider;
    }

    public override async Task<IndexDocumentsResponse> HandleAsync(IndexDocumentsRequest request, CancellationToken cancellationToken) {
        var validationResult = await ValidationService.ValidateAsync(
            request,
            cancellationToken
        ).SkipContextSync();

        if (!validationResult.Success) {
            return validationResult.AsErrors();
        }

        await NotificationService.NotifyStartingAsync()
                                 .SkipContextSync();

        var documentDirectoryWithDocuments = await GetDocumentDirectoryWithDocumentsAsync(
            request.DocumentDirectoryID,
            cancellationToken
        ).SkipContextSync();

        if (!documentDirectoryWithDocuments.Success) {
            return new IndexDocumentsMetadata(
                TotalDocuments: 0,
                TotalIndexed: 0
            );
        }

        var (documentDirectory, documents) = documentDirectoryWithDocuments.Value;

        await DeleteDocumentsFromIndexAsync(
            documentDirectory,
            cancellationToken
        ).SkipContextSync();

        return await InsertDocumentsIntoIndexAsync(
            documentDirectory,
            documents,
            cancellationToken
        ).SkipContextSync();
    }

    private async Task<Result<(DocumentDirectory, Document[])>> GetDocumentDirectoryWithDocumentsAsync(Guid documentDirectoryID, CancellationToken cancellationToken) {
        var documentDirectory = await AppDbContext.DocumentDirectories
                                                  .SingleOrDefaultAsync(entity => entity.ID == documentDirectoryID, cancellationToken)
                                                  .SkipContextSync();

        if (documentDirectory is null) {
            await NotificationService.NotifyMissingDocumentDirectoryAsync()
                                     .SkipContextSync();

            return Error.Missing("Document directory missing.");
        }

        var documents = AppDbContext.Documents
                                    .Where(entity => entity.DocumentDirectoryID == documentDirectory.ID)
                                    .ToArray();

        await NotificationService.NotifyDocumentsToIndexAsync(documents.Length)
                                 .SkipContextSync();

        return (documentDirectory, documents);
    }

    private async Task DeleteDocumentsFromIndexAsync(DocumentDirectory documentDirectory, CancellationToken cancellationToken) {
        var builder = CurrentIndex.CreateQueryBuilder();

        var definition = builder
            .WithField(Constants.Index.Fields.DocumentDirectoryID, [documentDirectory.ID.ToString("N")])
            .Build();

        var deleteDocumentsRequest = new DeleteDocumentsByQueryRequest(
            Query: definition.Query
        );

        var response = await CurrentIndex.DeleteAsync(deleteDocumentsRequest, cancellationToken);

        response.Match(
            onSuccess: _ => { },
            onFailure: errors => Logger.DeleteDocumentsFailure(errors[0].Message)
        );
    }

    private async Task<IndexDocumentsMetadata> InsertDocumentsIntoIndexAsync(DocumentDirectory documentDirectory, Document[] documents, CancellationToken cancellationToken) {
        var chunks = documents.Chunk(INDEX_CHUNK_SIZE);
        var count = 0;
        var indexingTime = TimeProvider.GetUtcNow().UtcDateTime;

        foreach (var chunk in chunks) {
            if (cancellationToken.IsCancellationRequested) { break; }

            await NotificationService.NotifyIndexingAsync(chunk)
                                     .SkipContextSync();

            chunk.Each(document => document.LastIndexingTime = indexingTime);

            var insertDocumentsRequest = new InsertDocumentsRequest(
                Documents: [.. chunk.Select(Map)]
            );
            var insertDocumentsResponse = await CurrentIndex.InsertAsync(insertDocumentsRequest, cancellationToken)
                                                            .SkipContextSync();

            count += await insertDocumentsResponse.Match(
                onSuccess: async value => {
                    await AppDbContext.SaveChangesAsync(cancellationToken);

                    return value.Count;
                },
                onFailure: errors => {
                    Logger.InsertDocumentsFailure(errors[0].Message);

                    return Task.FromResult(0);
                }
            );
        }

        var result = new IndexDocumentsMetadata(documents.Length, count);

        if (cancellationToken.IsCancellationRequested) {
            await NotificationService.NotifyCancellationAsync()
                                     .SkipContextSync();

            return result;
        }

        documentDirectory.LastIndexingTime = indexingTime;

        await AppDbContext.SaveChangesAsync(cancellationToken);

        await NotificationService.NotifyFinishAsync()
                                 .SkipContextSync();

        return result;
    }

    private static LuceneDocument Map(Document document) {
        return new LuceneDocument(id: document.ID.ToString("N"))
               .Set(
                   name: Constants.Index.Fields.DocumentDirectoryID,
                   value: document.DocumentDirectoryID.ToString("N")
               )
               .Set(
                   name: Constants.Index.Fields.FilePath,
                   value: document.FilePath
               )
               .Set(
                   name: Constants.Index.Fields.Content,
                   value: document.Content,
                   options: FieldOptions.Store | FieldOptions.Analyze
               )
               .Set(
                   name: Constants.Index.Fields.LastIndexingTime,
                   value: document.LastIndexingTime.GetValueOrDefault()
               );
    }
}
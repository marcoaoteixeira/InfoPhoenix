using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Entities;
using Nameless.InfoPhoenix.Extensions;
using Nameless.IO.FileSystem;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

public class FetchDocumentsForDocumentDirectoryRequestHandler : RequestHandlerBase<FetchDocumentsForDocumentDirectoryRequest, FetchDocumentsForDocumentDirectoryResponse> {
    private IFileSystem FileSystem { get; }
    private IDocumentService DocumentReaderManager { get; }

    public FetchDocumentsForDocumentDirectoryRequestHandler(
        AppDbContext appDbContext,
        INotificationService notificationService,
        IValidationService validationService,
        IFileSystem fileSystem,
        IDocumentService documentReaderManager,
        ILogger<FetchDocumentsForDocumentDirectoryRequestHandler> logger
    ) : base(
        appDbContext,
        notificationService,
        validationService,
        logger
    ) {
        FileSystem = fileSystem;
        DocumentReaderManager = documentReaderManager;
    }

    public override async Task<FetchDocumentsForDocumentDirectoryResponse> HandleAsync(FetchDocumentsForDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        var validationResult = await ValidationService.ValidateAsync(request, cancellationToken);

        if (!validationResult.Success) {
            return validationResult.AsErrors();
        }

        var documentDirectory = await AppDbContext.DocumentDirectories
                                                  .AsNoTracking()
                                                  .SingleOrDefaultAsync(item => item.ID == request.DocumentDirectoryID, cancellationToken)
                                                  .SkipContextSync();
        if (documentDirectory is null) {
            return Error.Missing("Não foi possível localizar o diretório de documentos.");
        }

        var fileSystemDocuments = GetFileSystemDocuments(documentDirectory).ToArray();
        var databaseDocuments = await GetDatabaseDocumentsAsync(documentDirectory, cancellationToken).SkipContextSync();

        try {
            var documentCount = await ProcessDocumentsAsync(fileSystemDocuments, databaseDocuments, cancellationToken).SkipContextSync();

            await NotificationService.NotifySuccessAsync(documentDirectory.Label, documentCount)
                                     .SkipContextSync();

            return new FetchDocumentsForDocumentDirectoryMetadata(documentCount);
        }
        catch (Exception ex) {
            await NotificationService.NotifyFailureAsync(ex.Message)
                                     .SkipContextSync();

            return Error.Failure(ex.Message);
        }
    }

    private IEnumerable<Document> GetFileSystemDocuments(DocumentDirectory documentDirectory) {
        return FileSystem.GetDirectory(documentDirectory.DirectoryPath)
                         .GetFiles(recursive: true)
                         .Where(FilterExtension)
                         .Select(file => new Document {
                             DocumentDirectoryID = documentDirectory.ID,
                             FilePath = file.Path,
                             LastWriteTime = file.LastWriteTime
                         });

        static bool FilterExtension(IFile file) {
            return Constants.WellKnownDocuments.AsArray.Contains(Path.GetExtension(file.Path));
        }
    }

    private async Task<Document[]> GetDatabaseDocumentsAsync(DocumentDirectory documentDirectory, CancellationToken cancellationToken) {
        return await AppDbContext.Documents
                                 .AsNoTracking()
                                 .Where(document => document.DocumentDirectoryID == documentDirectory.ID)
                                 .ToArrayAsync(cancellationToken)
                                 .SkipContextSync();
    }

    private async Task<int> ProcessDocumentsAsync(IEnumerable<Document> fileSystemDocuments, IEnumerable<Document> databaseDocuments, CancellationToken cancellationToken) {
        var databaseDocumentsDictionary = databaseDocuments.ToDictionary(
            keySelector: item => item.FilePath,
            elementSelector: item => item,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var fileSystemDocument in fileSystemDocuments) {
            var databaseDocument = databaseDocumentsDictionary.GetValueOrDefault(fileSystemDocument.FilePath);

            var content = await GetDocumentContentAsync(fileSystemDocument, cancellationToken).SkipContextSync();
            var raw = await GetDocumentRawAsync(fileSystemDocument, cancellationToken).SkipContextSync();

            // If databaseDocument is null, then it means that the fileSystemDocument
            // is a new document. Let's update it and add it as a new entry to the
            // DbContext.
            if (databaseDocument is null) {
                fileSystemDocument.ID = Guid.NewGuid();
                fileSystemDocument.Content = content;
                fileSystemDocument.RawFile = raw;

                // at this point it should have the DocumentDirectoryID filled.
                await AppDbContext.Documents
                                  .AddAsync(fileSystemDocument, cancellationToken)
                                  .ConfigureAwait(continueOnCapturedContext: false);

                await NotificationService.NotifyAddNewDocumentSuccessAsync(fileSystemDocument.FilePath)
                                         .SkipContextSync();

                continue;
            }

            if (databaseDocument.LastWriteTime == fileSystemDocument.LastWriteTime) {
                continue;
            }

            databaseDocument.Content = content;
            databaseDocument.RawFile = raw;
            databaseDocument.LastWriteTime = fileSystemDocument.LastWriteTime;
            databaseDocument.LastIndexingTime = null;

            AppDbContext.Documents.Update(databaseDocument);

            await NotificationService.NotifyDocumentUpdateSuccessAsync(databaseDocument.FilePath)
                                     .SkipContextSync();
        }

        return await AppDbContext.SaveChangesAsync(cancellationToken)
                                 .SkipContextSync();
    }

    private async Task<string> GetDocumentContentAsync(Document document, CancellationToken cancellationToken) {
        try {
            await NotificationService.NotifyReadingDocumentContentAsync(document.FilePath)
                                     .SkipContextSync();

            return await DocumentReaderManager.GetContentAsync(document.FilePath, cancellationToken)
                                              .SkipContextSync();
        }
        catch (Exception ex) {
            await NotificationService.NotifyReadDocumentContentFailureAsync(document.FilePath, ex.Message)
                                     .SkipContextSync();

            Logger.GetDocumentContentFailure(document.FilePath, ex);
        }

        return string.Empty;
    }

    private async Task<byte[]> GetDocumentRawAsync(Document document, CancellationToken cancellationToken) {
        try { return await File.ReadAllBytesAsync(document.FilePath, cancellationToken); }
        catch (Exception ex) { Logger.GetDocumentRawFailure(document.FilePath, ex); }

        return [];
    }
}

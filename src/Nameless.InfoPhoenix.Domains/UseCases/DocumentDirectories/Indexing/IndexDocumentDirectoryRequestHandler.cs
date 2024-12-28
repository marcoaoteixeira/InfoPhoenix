using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Domains.Entities;
using Nameless.InfoPhoenix.Notification;
using Nameless.Search;
using Document = Nameless.InfoPhoenix.Domains.Entities.Document;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Indexing;

public sealed class IndexDocumentDirectoryRequestHandler : IRequestHandler<IndexDocumentDirectoryRequest> {
    private const string NOTIFICATION_TITLE = "Indexação de Diretório de Documentos";
    private const string INDEXING_FINISHED_MSG_PATTERN = "Indexação concluída! Total de documentos: {0}";
    private const string DOCUMENT_DIRECTORY_NOT_FOUND_MSG = "Diretório de documentos não localizado.";
    private const string NO_DOCUMENTS_TO_INDEX_MSG = "Não existem documentos que necessitam de indexação";
    private const string INDEXING_DOCUMENT_MSG_PATTERN = "Indexando documento '{0}'...";
    private const string INDEXING_CANCELLED_BY_USER_MSG_PATTERN = "Indexação do diretório de documentos '{0}' cancelada pelo usuário.";
    private const string ERROR_WHILE_INDEXING_MSG_PATTERN = "Ocorreu um erro durante o processo de indexação dos documentos. Erro: {0}";
    private const string ERROR_WHILE_INDEXING_MSG = "Ocorreu um erro durante o processo de indexação dos documentos.";
    private const string NOTIFICATION_INDEXING_FINISHED_MSG_PATTERN = "Indexação do diretório de documentos '{0}' concluída. Total de documentos indexados: {1}";

    private readonly IClock _clock;
    private readonly IIndexProvider _indexProvider;
    private readonly INotificationService _notificationService;
    private readonly IRepository _repository;
    private readonly ILogger<IndexDocumentDirectoryRequestHandler> _logger;

    public IndexDocumentDirectoryRequestHandler(IClock clock,
                                                IIndexProvider indexProvider,
                                                INotificationService notificationService,
                                                IRepository repository,
                                                ILogger<IndexDocumentDirectoryRequestHandler> logger) {
        _clock = Prevent.Argument.Null(clock);
        _indexProvider = Prevent.Argument.Null(indexProvider);
        _notificationService = Prevent.Argument.Null(notificationService);
        _repository = Prevent.Argument.Null(repository);
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task Handle(IndexDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        var documentDirectory = await GetDocumentDirectoryAsync(request, cancellationToken);
        if (documentDirectory is null) {
            return;
        }

        var documents = await GetDocumentsAsync(request, cancellationToken);
        if (documents.Length == 0) {
            return;
        }

        var index = _indexProvider.CreateIndex(Constants.Common.IndexName);
        var now = _clock.GetUtcNow();

        documentDirectory.LastIndexingTime = now;
        documentDirectory.ModifiedAt = now;

        var indexDocuments = new List<IDocument>();
        foreach (var document in documents) {
            request.Reporter.Report(string.Format(INDEXING_DOCUMENT_MSG_PATTERN, Path.GetFileName(document.FilePath)));

            if (cancellationToken.IsCancellationRequested) {
                var message = string.Format(INDEXING_CANCELLED_BY_USER_MSG_PATTERN, documentDirectory.Label);

                await PushNotificationAsync(message, NotificationType.Warning);

                return;
            }

            document.LastIndexingTime = now;
            document.ModifiedAt = now;

            var indexDocument = index.NewDocument(document.ID.ToString())
                                     .Set(IndexFields.DocumentDirectoryID, documentDirectory.ID.ToString(), FieldOptions.Store)
                                     .Set(IndexFields.DocumentDirectoryLabel, documentDirectory.Label, FieldOptions.Store)
                                     .Set(IndexFields.DocumentDirectoryPath, documentDirectory.DirectoryPath, FieldOptions.Store)
                                     .Set(IndexFields.DocumentDirectoryOrder, documentDirectory.Order, FieldOptions.Store)
                                     .Set(IndexFields.DocumentDirectoryLastIndexingTime, documentDirectory.LastIndexingTime.GetValueOrDefault(), FieldOptions.Store)

                                     .Set(IndexFields.DocumentFileName, Path.GetFileNameWithoutExtension(document.FilePath), FieldOptions.Store)
                                     .Set(IndexFields.DocumentFilePath, document.FilePath, FieldOptions.Store)
                                     .Set(IndexFields.DocumentContent, document.Content, FieldOptions.Store | FieldOptions.Analyze)
                                     .Set(IndexFields.DocumentLastIndexingTime, document.LastIndexingTime.GetValueOrDefault(), FieldOptions.Store);

            indexDocuments.Add(indexDocument);
        }

        try { await index.StoreDocumentsAsync([.. indexDocuments], cancellationToken); }
        catch (Exception ex) {
            await PushNotificationAsync(string.Format(ERROR_WHILE_INDEXING_MSG_PATTERN, ex.Message),
                                        NotificationType.Error);

            _logger.LogError(ex, ERROR_WHILE_INDEXING_MSG);
        }

        await _repository.CommitChangesAsync(cancellationToken);

        request.Reporter.Report(string.Format(INDEXING_FINISHED_MSG_PATTERN, documents.Length));

        await PushNotificationAsync(string.Format(NOTIFICATION_INDEXING_FINISHED_MSG_PATTERN,
                                                  documentDirectory.Label,
                                                  documents.Length));
    }

    private async Task<DocumentDirectory?> GetDocumentDirectoryAsync(IndexDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        var documentDirectory = await _repository.GetQuery<DocumentDirectory>()
                                                 .SingleOrDefaultAsync(documentDirectory => documentDirectory.ID == request.DocumentDirectoryID,
                                                                       cancellationToken: cancellationToken);

        if (documentDirectory is not null) {
            return documentDirectory;
        }

        await PushNotificationAsync(DOCUMENT_DIRECTORY_NOT_FOUND_MSG, NotificationType.Warning);

        request.Reporter.Report(string.Format(INDEXING_FINISHED_MSG_PATTERN, 0));

        return null;
    }

    private async Task<Document[]> GetDocumentsAsync(IndexDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        var documents = await _repository.GetQuery<Document>()
                                         .Where(document => document.DocumentDirectoryID == request.DocumentDirectoryID &&
                                                            document.LastIndexingTime == null)
                                         .ToArrayAsync(cancellationToken);

        if (documents.Length > 0) {
            return documents;
        }

        await PushNotificationAsync(NO_DOCUMENTS_TO_INDEX_MSG, NotificationType.Information);

        request.Reporter.Report(string.Format(INDEXING_FINISHED_MSG_PATTERN, 0));

        return [];
    }

    private Task PushNotificationAsync(string message, NotificationType type = NotificationType.Success)
        => _notificationService.PublishAsync(new DocumentDirectoryIndexedNotification {
            Title = NOTIFICATION_TITLE,
            Message = message,
            Type = type
        });
}
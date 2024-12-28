using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Domains.Entities;
using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.UpdateContent;

public sealed class UpdateDocumentDirectoryContentRequestHandler : IRequestHandler<UpdateDocumentDirectoryContentRequest> {
    private static readonly string[] AllowedDocumentExtensions = [
        Constants.Documents.Extensions.TXT,
        Constants.Documents.Extensions.DOC,
        Constants.Documents.Extensions.DOCX,
        Constants.Documents.Extensions.RTF,
        Constants.Documents.Extensions.PDF
    ];

    private const string NOTIFICATION_TITLE = "Recolher Documentos do Diretório";

    private const string STARTING_FETCH_DOCUMENTS_FROM_DOCUMENT_DIRECTORY_MSG = "Iniciando a busca de documentos do diretório de documentos...";
    private const string GETTING_DOCUMENT_DIRECTORY_FROM_DATABASE_MSG = "Buscando diretório de documentos na base de dados...";
    private const string DOCUMENT_DIRECTORY_NOT_FOUND_MSG = "Diretório de documentos não localizando na base de dados.";

    private const string GETTING_DOCUMENTS_FROM_DATABASE_MSG = "Buscando documentos na base de dados...";
    private const string TOTAL_DOCUMENTS_FOUND_IN_DATABASE_MSG = "Total de documentos encontrados na base de dados: {0}";

    private const string GETTING_DOCUMENTS_FROM_FILESYSTEM_MSG = "Buscando documentos no disco...";
    private const string TOTAL_DOCUMENTS_FOUND_IN_FILESYSTEM_MSG = "Total de documentos encontrados no disco: {0}";

    private const string NEW_DOCUMENT_ADDED_MSG = "Novo documento adicionado ao diretório: '{0}'";
    private const string DOCUMENT_MARKED_TO_UPDATE_MSG = "Documento marcado para atualização: '{0}'";

    private const string FETCHING_DOCUMENT_RAW_FILE_MSG = "Recolhendo dados binários do documento: '{0}'";
    private const string UNABLE_TO_FETCH_DOCUMENT_CONTENT_MSG = "Não foi possível ler o conteúdo do documento: '{0}'";
    private const string FETCHING_DOCUMENT_CONTENT_MSG = "Recolhendo conteúdo do documento: '{0}'";

    private readonly IDocumentReaderManager _documentReaderManager;
    private readonly IFileSystem _fileSystem;
    private readonly INotificationService _notificationService;
    private readonly IRepository _repository;
    private readonly ILogger<UpdateDocumentDirectoryContentRequestHandler> _logger;

    public UpdateDocumentDirectoryContentRequestHandler(IDocumentReaderManager documentReaderManager,
                                                            IFileSystem fileSystem,
                                                            INotificationService notificationService,
                                                            IRepository repository,
                                                            ILogger<UpdateDocumentDirectoryContentRequestHandler> logger) {
        _documentReaderManager = Prevent.Argument.Null(documentReaderManager);
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _notificationService = Prevent.Argument.Null(notificationService);
        _repository = Prevent.Argument.Null(repository);
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task Handle(UpdateDocumentDirectoryContentRequest contentRequest,
                             CancellationToken cancellationToken) {
        _logger.StartingUpdateDocumentDirectoryContent(contentRequest.DocumentDirectoryID);

        contentRequest.Reporter.Report(STARTING_FETCH_DOCUMENTS_FROM_DOCUMENT_DIRECTORY_MSG);

        var documentDirectory = await GetDocumentDirectoryAsync(contentRequest, cancellationToken);
        if (documentDirectory is null) {
            _logger.DocumentDirectoryNotFound(contentRequest.DocumentDirectoryID);

            return;
        }

        var documentsFromDatabase = await GetDocumentsFromDatabaseAsync(contentRequest, cancellationToken);
        var documentsFromFileSystem = GetDocumentsFromFileSystem(contentRequest, documentDirectory);
        var documents = MergeDocuments(contentRequest, documentsFromDatabase, documentsFromFileSystem);

        FetchRawFile(contentRequest, documents);
        FetchContent(contentRequest, documents);

        await SaveDocumentsAsync(documents, cancellationToken);

        await PushNotificationAsync($"Busca de documentos para o diretório '{documentDirectory.Label}' concluída.", NotificationType.Success);
    }

    private async Task<DocumentDirectory?> GetDocumentDirectoryAsync(UpdateDocumentDirectoryContentRequest contentRequest,
                                                                     CancellationToken cancellationToken) {
        contentRequest.Reporter.Report(GETTING_DOCUMENT_DIRECTORY_FROM_DATABASE_MSG);

        var result = await _repository.GetQuery<DocumentDirectory>()
                                      .SingleOrDefaultAsync(documentDirectory => documentDirectory.ID == contentRequest.DocumentDirectoryID,
                                                            cancellationToken);

        if (result is null) {
            _logger.DocumentDirectoryNotFound(contentRequest.DocumentDirectoryID);

            contentRequest.Reporter.Report(DOCUMENT_DIRECTORY_NOT_FOUND_MSG);

            await PushNotificationAsync(DOCUMENT_DIRECTORY_NOT_FOUND_MSG);
        }

        return result;
    }

    private async Task<Document[]> GetDocumentsFromDatabaseAsync(UpdateDocumentDirectoryContentRequest contentRequest,
                                                                 CancellationToken cancellationToken) {
        contentRequest.Reporter.Report(GETTING_DOCUMENTS_FROM_DATABASE_MSG);

        var result = await _repository.GetQuery<Document>()
                                      .Where(document => document.DocumentDirectoryID == contentRequest.DocumentDirectoryID)
                                      .ToArrayAsync(cancellationToken);

        contentRequest.Reporter.Report(string.Format(TOTAL_DOCUMENTS_FOUND_IN_DATABASE_MSG, result.Length));

        return result;
    }

    private Document[] GetDocumentsFromFileSystem(UpdateDocumentDirectoryContentRequest contentRequest,
                                                  DocumentDirectory documentDirectory) {
        contentRequest.Reporter.Report(GETTING_DOCUMENTS_FROM_FILESYSTEM_MSG);

        var fileSystemDocuments = new List<Document>();
        foreach (var extension in AllowedDocumentExtensions) {
            var documents = _fileSystem.Directory
                                       .GetFiles(documentDirectory.DirectoryPath,
                                                 filter: $"*{extension}",
                                                 recursive: true)
                                       .Select(file => new Document {
                                           FilePath = file.Path,
                                           LastWriteTime = file.LastWriteAt,
                                           DocumentDirectoryID = documentDirectory.ID
                                       })
                                       .ToArray();

            fileSystemDocuments.AddRange(documents);
        }

        contentRequest.Reporter.Report(string.Format(TOTAL_DOCUMENTS_FOUND_IN_FILESYSTEM_MSG, fileSystemDocuments.Count));

        return [.. fileSystemDocuments];
    }

    private Document[] MergeDocuments(UpdateDocumentDirectoryContentRequest contentRequest,
                                      Document[] documentsFromDatabase,
                                      Document[] documentsFromFileSystem) {
        var mustInclude = new List<Document>();

        foreach (var fileSystemDocument in documentsFromFileSystem) {
            var databaseDocument = documentsFromDatabase.SingleOrDefault(document => string.Equals(document.FilePath,
                                                                                                   fileSystemDocument.FilePath,
                                                                                                   StringComparison.OrdinalIgnoreCase));

            if (databaseDocument is null) {
                contentRequest.Reporter.Report(string.Format(NEW_DOCUMENT_ADDED_MSG, _fileSystem.Path.GetFileName(fileSystemDocument.FilePath)));

                fileSystemDocument.DocumentDirectoryID = contentRequest.DocumentDirectoryID;
                fileSystemDocument.Content = string.Empty;
                fileSystemDocument.RawFile = [];
                fileSystemDocument.LastIndexingTime = null;

                mustInclude.Add(fileSystemDocument);
            }

            if (databaseDocument is not null && databaseDocument.LastWriteTime != fileSystemDocument.LastWriteTime) {
                contentRequest.Reporter.Report(string.Format(DOCUMENT_MARKED_TO_UPDATE_MSG, _fileSystem.Path.GetFileName(databaseDocument.FilePath)));

                databaseDocument.Content = string.Empty;
                databaseDocument.RawFile = [];
                databaseDocument.LastIndexingTime = null;
                databaseDocument.LastWriteTime = fileSystemDocument.LastWriteTime;
            }
        }

        mustInclude.AddRange(documentsFromDatabase);

        return [.. mustInclude];
    }

    private void FetchRawFile(UpdateDocumentDirectoryContentRequest contentRequest, 
                              Document[] documents) {
        foreach (var document in documents) {
            if (document.RawFile.Length != 0) {
                continue;
            }

            contentRequest.Reporter.Report(string.Format(FETCHING_DOCUMENT_RAW_FILE_MSG, _fileSystem.Path.GetFileName(document.FilePath)));

            document.RawFile = _fileSystem.File.ReadAllBytes(document.FilePath);
        }
    }

    private void FetchContent(UpdateDocumentDirectoryContentRequest contentRequest,
                              Document[] documents) {
        foreach (var document in documents) {
            if (document.Content != string.Empty) {
                continue;
            }

            contentRequest.Reporter.Report(string.Format(FETCHING_DOCUMENT_CONTENT_MSG, _fileSystem.Path.GetFileName(document.FilePath)));

            var contentResult = _documentReaderManager.GetDocumentContent(document.FilePath);
            if (contentResult.HasErrors) {
                _logger.UnableToUpdateDocumentDirectoryContent(document.FilePath, contentResult.Errors.First().Description);

                contentRequest.Reporter.Report(string.Format(UNABLE_TO_FETCH_DOCUMENT_CONTENT_MSG, _fileSystem.Path.GetFileName(document.FilePath)));
                
                continue;
            }

            document.Content = contentResult.Value;
        }
    }

    private Task PushNotificationAsync(string message, NotificationType type = NotificationType.Information)
        => _notificationService.PublishAsync(new UpdateDocumentDirectoryContentCompletedNotification {
            Title = NOTIFICATION_TITLE,
            Message = message,
            Type = type
        });

    private async Task SaveDocumentsAsync(Document[] documents, CancellationToken cancellationToken) {
        foreach (var document in documents) {
            await _repository.SaveAsync(document, cancellationToken);
        }

        await _repository.CommitChangesAsync(cancellationToken);
    }
}
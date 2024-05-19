using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Entities;
using Nameless.InfoPhoenix.Domain.Objects;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Office;
using Nameless.InfoPhoenix.Responses;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class CollectDocumentsFromDocumentDirectoryRequestHandler : IRequestHandler<CollectDocumentsFromDocumentDirectoryRequest, EmptyResponse> {
        #region Private Read-Only Fields

        private readonly AppDbContext _appDbContext;
        private readonly ILogger _logger;
        private readonly IOfficeSuite _officeSuite;
        private readonly IPubSubService _pubSubService;

        #endregion

        #region Public Constructors

        public CollectDocumentsFromDocumentDirectoryRequestHandler(AppDbContext appDbContext, ILogger<CollectDocumentsFromDocumentDirectoryRequestHandler> logger, IOfficeSuite officeSuite, IPubSubService pubSubService) {
            _appDbContext = Guard.Against.Null(appDbContext, nameof(appDbContext));
            _logger = Guard.Against.Null(logger, nameof(logger));
            _officeSuite = Guard.Against.Null(officeSuite, nameof(officeSuite));
            _pubSubService = Guard.Against.Null(pubSubService, nameof(pubSubService));
        }

        #endregion

        #region IRequestHandler<CollectDocumentsFromDocumentDirectoryRequest, EmptyResponse> Members

        public async Task<EmptyResponse> Handle(CollectDocumentsFromDocumentDirectoryRequest request, CancellationToken cancellationToken) {
            try {
                var documentDirectory = await FetchDocumentDirectoryAsync(request.DocumentDirectoryID,
                                                                          cancellationToken);

                if (documentDirectory is null) {
                    return new EmptyResponse {
                        Error = Root.DbErrors.DOCUMENT_DIRECTORY_NOT_FOUND
                    };
                }

                var fileSystemDocuments = await FetchFileSystemDocumentsAsync(documentDirectory, cancellationToken);
                var databaseDocumentsDictionary = await FetchDatabaseDocumentsAsDictionaryAsync(documentDirectory, cancellationToken);
                var mustIncludeDocuments = await ExtractMustIncludeDocumentsAsync(fileSystemDocuments, databaseDocumentsDictionary, cancellationToken);
                var documents = await CreateDocumentsAsync(mustIncludeDocuments, cancellationToken);
                await PersistDocumentsAsync(documents, cancellationToken);
            }
            catch (OperationCanceledException) {
                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = Root.DbErrors.COLLECT_DOCUMENTS_OPERATION_CANCELLED
                });

                _logger.LogInformation("User request task cancellation.");

                return new EmptyResponse {
                    Error = Root.DbErrors.COLLECT_DOCUMENTS_OPERATION_CANCELLED
                };
            }

            return new EmptyResponse();
        }

        #endregion

        #region Private Static Methods

        private async Task<DocumentDirectoryDto?> FetchDocumentDirectoryAsync(Guid documentDirectoryID, CancellationToken cancellationToken) {
            await _pubSubService.PublishAsync(new CollectDocumentNotification {
                Message = "Localizando Diretório de Documentos na Base de Dados..."
            });

            _logger.LogInformation(message: "Fetching Document Directory. ID: {DocumentDirectoryID}",
                                   args: [documentDirectoryID]);

            cancellationToken.ThrowIfCancellationRequested();

            var documentDirectory = await _appDbContext
                                          .DocumentDirectories
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(documentDirectory => documentDirectory.ID == documentDirectoryID, cancellationToken);

            if (documentDirectory is null) {
                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = "Diretório de Documentos não localizado."
                });

                _logger.LogWarning(message: "Document Directory not found. ID: {DocumentDirectoryID}",
                                   args: [documentDirectoryID]);

                return null;
            }

            _logger.LogInformation(message: "Document Directory found: [{Label}].",
                                   args: [documentDirectory.Label]);

            return documentDirectory.ToDto();
        }

        private async Task<DocumentDto[]> FetchFileSystemDocumentsAsync(DocumentDirectoryDto documentDirectory, CancellationToken cancellationToken) {
            await _pubSubService.PublishAsync(new CollectDocumentNotification {
                Message = "Localizando Documentos no Sistema de Arquivos..."
            });

            _logger.LogInformation(message: "[{DocumentDirectory}] Fetching documents from file system.",
                                   args: [documentDirectory.Label]);

            cancellationToken.ThrowIfCancellationRequested();

            var documents = Root
                                .Defaults
                                .ValidDocumentExtensions
                                .SelectMany(extension =>
                                                Directory
                                                    .GetFiles(documentDirectory.DirectoryPath, $"*{extension}", SearchOption.AllDirectories)
                                                    .Select(filePath => new FileInfo(filePath))
                                )
                                .Select(file => new DocumentDto {
                                    FilePath = file.FullName,
                                    LastWriteTime = file.LastWriteTime,
                                    DocumentDirectory = documentDirectory
                                })
                                .ToArray();

            _logger.LogInformation(message: "[{DocumentDirectory}] Document count (file system): {DocumentCount}",
                                   args: new object[] { documentDirectory.Label, documents.Length });

            return documents;
        }

        private async Task<Dictionary<string, DocumentDto>> FetchDatabaseDocumentsAsDictionaryAsync(DocumentDirectoryDto documentDirectory, CancellationToken cancellationToken) {
            await _pubSubService.PublishAsync(new CollectDocumentNotification {
                Message = "Localizando Documentos na Base de Dados..."
            });

            _logger.LogInformation(message: "[{DocumentDirectory}] Fetching Document records from database",
                                   args: [documentDirectory.Label]);

            cancellationToken.ThrowIfCancellationRequested();

            var result = _appDbContext
                         .Documents
                         .AsNoTracking()
                         .Where(document => document.DocumentDirectoryID == documentDirectory.ID)
                         .ToDictionary(keySelector: document => document.FilePath,
                                       elementSelector: document => document.ToDto(documentDirectory));

            _logger.LogInformation(message: "[{DocumentDirectory}] document count (database): {DocumentCount}",
                                   args: new object[] { documentDirectory.Label, result.Count });

            return result;
        }

        private async Task<DocumentDto[]> ExtractMustIncludeDocumentsAsync(IEnumerable<DocumentDto> fileSystemDocuments, IDictionary<string, DocumentDto> databaseDocumentsAsDictionary, CancellationToken cancellationToken) {
            await _pubSubService.PublishAsync(new CollectDocumentNotification {
                Message = "Selecionando Documentos no Sistema de Arquivos para atualização..."
            });

            cancellationToken.ThrowIfCancellationRequested();

            var result = new List<DocumentDto>();
            foreach (var fileSystemDocument in fileSystemDocuments) {
                if (databaseDocumentsAsDictionary.TryGetValue(fileSystemDocument.FilePath, out var databaseDocument)) {
                    if (fileSystemDocument.LastWriteTime > databaseDocument.LastWriteTime) {
                        result.Add(databaseDocument with {
                            LastIndexingTime = null,
                            LastWriteTime = fileSystemDocument.LastWriteTime
                        });
                    }
                }
                else { result.Add(fileSystemDocument); }
            }

            return [.. result];
        }
        
        private async Task<Document[]> CreateDocumentsAsync(IEnumerable<DocumentDto> mustIncludeDocumentDtos, CancellationToken cancellationToken) {
            var result = new List<Document>();

            foreach (var mustIncludeDocumentDto in mustIncludeDocumentDtos) {
                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = $"Criando Documento para arquivo {mustIncludeDocumentDto.FileName}..."
                });

                _logger.LogInformation(message: "Building document: {FilePath}",
                                       args: [mustIncludeDocumentDto.FilePath]);

                cancellationToken.ThrowIfCancellationRequested();

                var document = new Document {
                    ID = mustIncludeDocumentDto.ID,
                    DocumentDirectoryID = mustIncludeDocumentDto.DocumentDirectory.ID,
                    FilePath = mustIncludeDocumentDto.FilePath,
                    LastIndexingTime = null,
                    LastWriteTime = mustIncludeDocumentDto.LastWriteTime,
                    CreatedAt = mustIncludeDocumentDto.CreatedAt != DateTime.MinValue
                        ? mustIncludeDocumentDto.CreatedAt
                        : DateTime.Now,
                    ModifiedAt = mustIncludeDocumentDto.ModifiedAt
                };

                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = $"Lendo conteúdo do arquivo {mustIncludeDocumentDto.FileName}..."
                });
                if (!TryFetchContentForDocument(document.FilePath, out var content)) {
                    await _pubSubService.PublishAsync(new CollectDocumentNotification {
                        Message = $"Erro ao ler o Documento {mustIncludeDocumentDto.FileName}. Documento ignorado."
                    });

                    _logger.LogInformation(message: "Could not read document content: {FilePath}",
                                           args: [mustIncludeDocumentDto.FilePath]);
                    continue;
                }
                document.Content = content;

                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = $"Criando backup do Documento {mustIncludeDocumentDto.FileName}..."
                });
                if (!TryFetchRawFileForDocument(document.FilePath, out var rawFile)) {
                    await _pubSubService.PublishAsync(new CollectDocumentNotification {
                        Message = $"Erro ao criar backup do Documento {mustIncludeDocumentDto.FileName}."
                    });

                    _logger.LogInformation(message: "Could not read document bytes: {FilePath}",
                                           args: [mustIncludeDocumentDto.FilePath]);

                    rawFile = [];
                }
                document.RawFile = rawFile;

                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = $"Documento criado a partir do arquivo {mustIncludeDocumentDto.FileName}."
                });

                _logger.LogInformation(message: "Document built [{FilePath}].",
                                       args: [document.FilePath]);

                result.Add(document);
            }

            return [.. result];
        }

        private bool TryFetchContentForDocument(string filePath, [NotNullWhen(returnValue: true)] out string? content) {
            content = null;

            try {
                content = Path.GetExtension(filePath)
                              .Equals(Root.Defaults.TXT_EXTENSION, StringComparison.InvariantCultureIgnoreCase)
                    ? File.ReadAllText(filePath)
                    : _officeSuite.GetWordDocumentContent(filePath, formatted: true);
            }
            catch (Exception ex) {
                _logger.LogError(exception: ex,
                                 message: "Unable read document using Office Suite service. File: {FilePath}",
                                 args: [filePath]);
                return false;
            }

            return true;
        }

        private bool TryFetchRawFileForDocument(string filePath, [NotNullWhen(returnValue: true)] out byte[]? rawFile) {
            rawFile = null;

            try { rawFile = File.ReadAllBytes(filePath); }
            catch (Exception ex) {
                _logger.LogError(exception: ex,
                                 message: "Unable to read file bytes. File: {FilePath}",
                                 args: filePath
                );

                return false;
            }

            return true;
        }

        private async Task PersistDocumentsAsync(IEnumerable<Document> documents, CancellationToken cancellationToken) {
            var now = DateTime.Now;
            foreach (var document in documents) {
                await _pubSubService.PublishAsync(new CollectDocumentNotification {
                    Message = $"Adicionando Documento [{Path.GetFileNameWithoutExtension(document.FilePath)}] na Base de Dados..."
                });

                cancellationToken.ThrowIfCancellationRequested();

                var dbDoc = await _appDbContext
                            .Documents
                            .FindAsync([document.ID], cancellationToken);

                if (dbDoc is null) {
                    document.CreatedAt = now;

                    await _appDbContext
                          .Documents
                          .AddAsync(document, cancellationToken);

                    continue;
                }

                dbDoc.DocumentDirectoryID = document.DocumentDirectoryID;
                dbDoc.FilePath = document.FilePath;
                dbDoc.Content = document.Content;
                dbDoc.RawFile = document.RawFile;
                dbDoc.LastIndexingTime = document.LastIndexingTime;
                dbDoc.LastWriteTime = document.LastWriteTime;
                dbDoc.ModifiedAt = now;
            }

            var total = await _appDbContext.SaveChangesAsync(cancellationToken);

            await _pubSubService.PublishAsync(new CollectDocumentNotification {
                Message = $"Total de {total} Documentos salvos na Base de Dados."
            });
        }

        #endregion
    }
}

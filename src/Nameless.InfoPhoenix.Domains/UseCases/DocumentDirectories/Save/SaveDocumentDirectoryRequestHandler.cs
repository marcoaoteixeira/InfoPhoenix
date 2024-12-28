using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Domains.Entities;
using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;

public class SaveDocumentDirectoryRequestHandler : IRequestHandler<SaveDocumentDirectoryRequest> {
    private const string DOCUMENT_DIRECTORY_CREATION_SUCCESSFUL_MSG = "O diretório de documentos '{0}' foi criado com sucesso.";
    private const string DOCUMENT_DIRECTORY_CREATION_FAILURE_MSG = "Ocorreu um erro ao criar o diretório de documentos '{0}'. Erro: {1}";

    private const string DOCUMENT_DIRECTORY_EDITION_SUCCESSFUL_MSG = "O diretório de documentos '{0}' foi editado com sucesso.";
    private const string DOCUMENT_DIRECTORY_EDITION_FAILURE_MSG = "Ocorreu um erro ao editar o diretório de documentos '{0}'. Erro: {1}";

    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IRepository _repository;
    private readonly ILogger<SaveDocumentDirectoryRequestHandler> _logger;

    public SaveDocumentDirectoryRequestHandler(IClock clock,
                                               INotificationService notificationService,
                                               IRepository repository,
                                               ILogger<SaveDocumentDirectoryRequestHandler> logger) {
        _clock = Prevent.Argument.Null(clock);
        _notificationService = Prevent.Argument.Null(notificationService);
        _repository = Prevent.Argument.Null(repository);
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task Handle(SaveDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        try {
            var now = _clock.GetUtcNow();
            var current = await _repository.GetQuery<DocumentDirectory>()
                                           .SingleOrDefaultAsync(documentDirectory => documentDirectory.ID == request.ID,
                                                                 cancellationToken) ?? new DocumentDirectory();

            current.Label = request.Label;

            // if current directory path is different from
            // the new directory path, we need to set LastIndexingTime
            // to null, since we'll need to index it again.
            if (!string.Equals(current.DirectoryPath, request.DirectoryPath, StringComparison.OrdinalIgnoreCase)) {
                current.LastIndexingTime = null;
            }

            current.DirectoryPath = request.DirectoryPath;
            current.Order = request.Order;

            if (current.CreatedAt == DateTime.MinValue) {
                current.CreatedAt = now;
            }
            current.ModifiedAt = now;

            await _repository.SaveAsync(current, cancellationToken);
            await _repository.CommitChangesAsync(cancellationToken);

            var message = request.ID == Guid.Empty
                ? string.Format(DOCUMENT_DIRECTORY_CREATION_SUCCESSFUL_MSG, request.Label)
                : string.Format(DOCUMENT_DIRECTORY_EDITION_SUCCESSFUL_MSG, request.Label);

            await PushNotification(message, NotificationType.Success, request.ID);
        } catch (Exception ex) {
            var creating = request.ID == Guid.Empty;

            _logger.OnCondition(creating).CreateDocumentDirectoryFailed(ex);
            _logger.OnCondition(!creating).EditDocumentDirectoryFailed(request.ID, ex);

            var message = creating
                ? string.Format(DOCUMENT_DIRECTORY_CREATION_FAILURE_MSG, request.Label, ex.Message)
                : string.Format(DOCUMENT_DIRECTORY_EDITION_FAILURE_MSG, request.Label, ex.Message);

            await PushNotification(message, NotificationType.Error, request.ID);
        }
    }

    private Task PushNotification(string message, NotificationType type, Guid id)
        => _notificationService.PublishAsync(new DocumentDirectorySavedNotification {
            Title = $"{(id == Guid.Empty ? "Criação" : "Edição")}: Diretório de Documentos",
            Message = message,
            Type = type,
            Id = id
        });
}
using MediatR;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Domains.Entities;
using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Delete;

public class DeleteDocumentDirectoryRequestHandler : IRequestHandler<DeleteDocumentDirectoryRequest> {
    private const string DOCUMENT_DIRECTORY_DELETED_SUCCESSFUL_MSG = "O diretório de documentos '{0}' foi removido com sucesso.";
    private const string NO_DOCUMENT_DIRECTORY_TO_DELETE_MSG = "Não foi localizado o registro para o diretório de documentos '{0}'. Remoção cancelada.";
    private const string DOCUMENT_DIRECTORY_DELETED_FAILURE_MSG = "Ocorreu um erro ao remover o diretório de documentos '{0}'. Erro: {1}";

    private readonly INotificationService _notificationService;
    private readonly IRepository _repository;
    private readonly ILogger<DeleteDocumentDirectoryRequestHandler> _logger;

    public DeleteDocumentDirectoryRequestHandler(INotificationService notificationService,
                                                 IRepository repository,
                                                 ILogger<DeleteDocumentDirectoryRequestHandler> logger) {
        _notificationService = Prevent.Argument.Null(notificationService);
        _repository = Prevent.Argument.Null(repository);
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task Handle(DeleteDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        try {
            var count = await _repository.DeleteAsync<DocumentDirectory>(request.ID, cancellationToken);
            var message = count > 0
                ? string.Format(DOCUMENT_DIRECTORY_DELETED_SUCCESSFUL_MSG, request.Label)
                : string.Format(NO_DOCUMENT_DIRECTORY_TO_DELETE_MSG, request.Label);

            await PushNotification(message, count > 0 ? NotificationType.Success : NotificationType.Warning);
        } catch (Exception ex) {
            _logger.DeleteDocumentDirectoryFailed(ex);

            var message = string.Format(DOCUMENT_DIRECTORY_DELETED_FAILURE_MSG, request.Label, ex.Message);

            await PushNotification(message, NotificationType.Error);
        }
    }

    private Task PushNotification(string message, NotificationType type)
        => _notificationService.PublishAsync(new DocumentDirectoryDeletedNotification {
            Title = "Remover Diretório de Documentos",
            Message = message,
            Type = type
        });
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Extensions;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

public class DeleteDocumentDirectoryRequestHandler(
    AppDbContext appDbContext,
    INotificationService notificationService,
    IValidationService validationService,
    ILogger<DeleteDocumentDirectoryRequestHandler> logger
) : RequestHandlerBase<DeleteDocumentDirectoryRequest, DeleteDocumentDirectoryResponse>(
    appDbContext,
    notificationService,
    validationService,
    logger
) {
    public override async Task<DeleteDocumentDirectoryResponse> HandleAsync(DeleteDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        var validationResult = await ValidationService.ValidateAsync(request, cancellationToken);

        if (!validationResult.Success) {
            return validationResult.AsErrors();
        }

        var documentDirectory = await AppDbContext.DocumentDirectories
                                                  .SingleOrDefaultAsync(item => item.ID == request.DocumentDirectoryID, cancellationToken)
                                                  .SkipContextSync();

        if (documentDirectory is null) {
            return Error.Missing("Não foi possível localizar o diretório de documentos.");
        }

        try {
            AppDbContext.DocumentDirectories.Remove(documentDirectory);

            await AppDbContext.SaveChangesAsync(cancellationToken)
                              .SkipContextSync();

            await NotificationService.NotifySuccessAsync(documentDirectory.Label)
                                     .SkipContextSync();

            return new DeleteDocumentDirectoryMetadata(documentDirectory.ID);
        }
        catch (Exception ex) {
            await NotificationService.NotifyFailureAsync(ex.Message)
                                     .SkipContextSync();

            Logger.DeleteDocumentDirectoryFailure(documentDirectory.Label, ex);

            return Error.Failure(ex.Message);
        }
    }
}
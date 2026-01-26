using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Extensions;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

public class UpdateDocumentDirectoryRequestHandler(
    AppDbContext appDbContext,
    INotificationService notificationService,
    IValidationService validationService,
    ILogger<UpdateDocumentDirectoryRequestHandler> logger
) : RequestHandlerBase<UpdateDocumentDirectoryRequest, UpdateDocumentDirectoryResponse>(
    appDbContext,
    notificationService,
    validationService,
    logger
) {
    public override async Task<UpdateDocumentDirectoryResponse> HandleAsync(UpdateDocumentDirectoryRequest request, CancellationToken cancellationToken) {
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
            var sameDirectoryPath = string.Equals(documentDirectory.DirectoryPath, request.DirectoryPath, StringComparison.OrdinalIgnoreCase);
            if (!sameDirectoryPath) {
                documentDirectory.LastIndexingTime = null;
            }

            documentDirectory.Label = request.Label;
            documentDirectory.DirectoryPath = request.DirectoryPath;
            documentDirectory.DisplayPosition = request.DisplayPosition;

            await AppDbContext.SaveChangesAsync(cancellationToken)
                              .SkipContextSync();

            await NotificationService.NotifySuccessAsync(documentDirectory.Label)
                                     .SkipContextSync();

            return new UpdateDocumentDirectoryMetadata(documentDirectory.ID);
        }
        catch (Exception ex) {
            await NotificationService.NotifyFailureAsync(ex.Message)
                                     .SkipContextSync();

            Logger.UpdateDocumentDirectoryFailure(request.Label, ex);

            return Error.Failure(ex.Message);
        }
    }
}
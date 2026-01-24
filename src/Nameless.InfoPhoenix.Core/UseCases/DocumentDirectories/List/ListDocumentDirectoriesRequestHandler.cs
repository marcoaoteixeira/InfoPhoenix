using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.DTOs;
using Nameless.InfoPhoenix.Extensions;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.List;

public class ListDocumentDirectoriesRequestHandler(
    AppDbContext appDbContext,
    INotificationService notificationService,
    IValidationService validationService,
    ILogger<ListDocumentDirectoriesRequestHandler> logger
) : RequestHandlerBase<ListDocumentDirectoriesRequest, ListDocumentDirectoriesResponse>(
    appDbContext,
    notificationService,
    validationService,
    logger
) {
    public override async Task<ListDocumentDirectoriesResponse> HandleAsync(ListDocumentDirectoriesRequest request, CancellationToken cancellationToken) {
        var validationResult = await ValidationService.ValidateAsync(request, cancellationToken);

        if (!validationResult.Succeeded) {
            return validationResult.AsErrors();
        }

        try {
            var entries =
                from documentDirectory in AppDbContext.DocumentDirectories.AsNoTracking()
                join document in AppDbContext.Documents.AsNoTracking()
                    on documentDirectory.ID equals document.DocumentDirectoryID
                    into documents
                select new DocumentDirectoryDto {
                    ID = documentDirectory.ID,
                    Label = documentDirectory.Label,
                    DirectoryPath = documentDirectory.DirectoryPath,
                    DisplayPosition = documentDirectory.DisplayPosition,
                    LastIndexingTime = documentDirectory.LastIndexingTime,
                    DocumentCount = documents.Count()
                };

            return entries.ToArray();
        }
        catch (Exception ex) {
            Logger.ListDocumentDirectoriesFailure(ex);

            return Error.Failure(ex.Message);
        }
    }
}
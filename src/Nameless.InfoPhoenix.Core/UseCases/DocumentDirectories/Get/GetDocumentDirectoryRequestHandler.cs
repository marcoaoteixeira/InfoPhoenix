using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.DTOs;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Get;

public class GetDocumentDirectoryRequestHandler(
    AppDbContext appDbContext,
    INotificationService notificationService,
    IValidationService validationService,
    ILogger<GetDocumentDirectoryRequestHandler> logger
) : RequestHandlerBase<GetDocumentDirectoryRequest, GetDocumentDirectoryResponse>(
    appDbContext,
    notificationService,
    validationService,
    logger
) {
    public override async Task<GetDocumentDirectoryResponse> HandleAsync(GetDocumentDirectoryRequest request,
        CancellationToken cancellationToken) {
        try {
            var documentDirectory = await AppDbContext.DocumentDirectories
                                                      .AsNoTracking()
                                                      .SingleOrDefaultAsync(
                                                          item => item.ID == request.DocumentDirectoryID,
                                                          cancellationToken);

            if (documentDirectory is null) {
                return Error.Missing("Não foi possível localizar o diretório de documentos.");
            }

            var documentCount = await GetDocumentCountAsync(documentDirectory.ID).SkipContextSync();

            return new DocumentDirectoryDto(documentDirectory) {
                DocumentCount = documentCount
            };

        }
        catch (Exception ex) {
            Logger.GetDocumentDirectoryFailure(request.DocumentDirectoryID, ex);

            return Error.Failure(ex.Message);
        }

        Task<int> GetDocumentCountAsync(Guid documentDirectoryID) {
            return AppDbContext.Documents
                               .AsNoTracking()
                               .CountAsync(document => document.DocumentDirectoryID == documentDirectoryID, cancellationToken);
        }
    }
}
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Entities;
using Nameless.InfoPhoenix.Extensions;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

public class CreateNewDocumentDirectoryRequestHandler(
    AppDbContext appDbContext,
    INotificationService notificationService,
    IValidationService validationService,
    ILogger<CreateNewDocumentDirectoryRequestHandler> logger
) : RequestHandlerBase<CreateNewDocumentDirectoryRequest, CreateNewDocumentDirectoryResponse>(
    appDbContext,
    notificationService,
    validationService,
    logger
) {
    public override async Task<CreateNewDocumentDirectoryResponse> HandleAsync(CreateNewDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        var validationResult = await ValidationService.ValidateAsync(request, cancellationToken);

        if (!validationResult.Success) {
            return validationResult.AsErrors();
        }

        var documentDirectory = new DocumentDirectory {
            ID = Guid.NewGuid(),
            Label = Guard.Against.NullOrWhiteSpace(request.Label),
            DirectoryPath = Guard.Against.NullOrWhiteSpace(request.DirectoryPath),
            DisplayPosition = Guard.Against.LowerThan(request.DisplayPosition, compare: 0),
            LastIndexingTime = null
        };

        try {
            var entry = await AppDbContext.DocumentDirectories
                                          .AddAsync(documentDirectory, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext: false);

            await AppDbContext.SaveChangesAsync(cancellationToken)
                              .SkipContextSync();

            await NotificationService.NotifySuccessAsync(entry.Entity.Label)
                                     .SkipContextSync();

            return new CreateNewDocumentDirectoryMetadata(entry.Entity.ID);
        }
        catch (Exception ex) {
            await NotificationService.NotifyFailureAsync(ex.Message)
                                     .SkipContextSync();

            Logger.CreateDocumentDirectoryFailure(request.Label, ex);

            return Error.Failure(ex.Message);
        }
    }
}
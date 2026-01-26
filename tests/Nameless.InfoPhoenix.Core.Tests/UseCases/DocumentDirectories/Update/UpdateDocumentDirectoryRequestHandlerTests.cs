using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Entities;
using Nameless.InfoPhoenix.Testing.Tools;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Core;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Notifications;
using Nameless.Testing.Tools.Attributes;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

[UnitTest]
public class UpdateDocumentDirectoryRequestHandlerTests {
    private static readonly Guid DocumentDirectoryID = Guid.NewGuid();

    private static UpdateDocumentDirectoryRequestHandler CreateSut(
        AppDbContext appDbContext,
        INotificationService notificationService = null,
        IValidationService validationService = null,
        ILogger<UpdateDocumentDirectoryRequestHandler> logger = null) {

        notificationService ??= new NotificationServiceMocker().Build();
        validationService ??= new ValidationServiceMocker().WithValidateAsync(ValidationResult.Create())
                                                           .Build();
        logger ??= NullLogger<UpdateDocumentDirectoryRequestHandler>.Instance;

        return new UpdateDocumentDirectoryRequestHandler(
            appDbContext,
            notificationService,
            validationService,
            logger
        );
    }

    [Fact]
    public async Task Happy_Path() {
        // arrange
        var notifications = new List<UpdateDocumentDirectoryNotification>();
        var notificationService = new NotificationServiceMocker().WithPublishAsync<UpdateDocumentDirectoryNotification>(notifications.Add)
                                                                 .Build();
        await using var appDbContext = await AppDbContextFactory.CreateAsync(opts => {
            opts.UseSeeding(CreateDocumentDirectoryAsync);
        }, TestContext.Current.CancellationToken);

        var currentDocumentDirectory = appDbContext.DocumentDirectories.AsNoTracking().Single();

        var sut = CreateSut(appDbContext, notificationService);
        var request = new UpdateDocumentDirectoryRequest {
            DocumentDirectoryID = DocumentDirectoryID,
            Label = nameof(UpdateDocumentDirectoryRequestHandlerTests),
            DirectoryPath = Path.GetTempPath(),
            DisplayPosition = 2
        };

        // act
        var response = await sut.HandleAsync(request, CancellationToken.None);

        var updatedDocumentDirectory = appDbContext.DocumentDirectories.AsNoTracking().Single();

        // assert
        response.Match(
            onSuccess: _ => Assert.Multiple(() => {
                Assert.Equal(DocumentDirectoryID, response.Value.DocumentDirectoryID);

                Assert.NotEmpty(notifications);
                Assert.Equal(NotificationType.Success, notifications[0].Type);

                Assert.NotEqual(currentDocumentDirectory.Label, updatedDocumentDirectory.Label);
                Assert.NotEqual(currentDocumentDirectory.DirectoryPath, updatedDocumentDirectory.DirectoryPath);
                Assert.NotEqual(currentDocumentDirectory.DisplayPosition, updatedDocumentDirectory.DisplayPosition);
            }),
            onFailure: errors => Assert.Fail($"Expected success but got failure: {errors.ToJson()}")
        );
    }

    private static async Task CreateDocumentDirectoryAsync(AppDbContext dbContext, bool state, CancellationToken cancellationToken) {
        var documentDirectory = new DocumentDirectory {
            ID = DocumentDirectoryID,
            Label = nameof(DocumentDirectory),
            DirectoryPath = typeof(UpdateDocumentDirectoryRequestHandlerTests).Assembly.GetDirectoryPath(),
            DisplayPosition = 1
        };

        await dbContext.DocumentDirectories.AddAsync(documentDirectory, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

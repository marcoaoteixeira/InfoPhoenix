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

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

[UnitTest]
public class DeleteDocumentDirectoryRequestHandlerTests {
    private static readonly Guid DocumentDirectoryID = Guid.NewGuid();

    private static DeleteDocumentDirectoryRequestHandler CreateSut(
        AppDbContext appDbContext,
        INotificationService notificationService = null,
        IValidationService validationService = null,
        ILogger<DeleteDocumentDirectoryRequestHandler> logger = null) {

        notificationService ??= new NotificationServiceMocker().Build();
        logger ??= NullLogger<DeleteDocumentDirectoryRequestHandler>.Instance;
        validationService ??= new ValidationServiceMocker().WithValidateAsync(ValidationResult.Create())
                                                           .Build();

        return new DeleteDocumentDirectoryRequestHandler(
            appDbContext,
            notificationService,
            validationService,
            logger
        );
    }

    [Fact]
    public async Task Happy_Path() {
        // arrange
        var notifications = new List<DeleteDocumentDirectoryNotification>();
        var notificationService = new NotificationServiceMocker().WithPublishAsync<DeleteDocumentDirectoryNotification>(notifications.Add)
                                                                 .Build();
        await using var appDbContext = await AppDbContextFactory.CreateAsync(opts => {
            opts.UseSeeding(CreateDocumentDirectoryAsync);
        }, TestContext.Current.CancellationToken);

        var sut = CreateSut(appDbContext, notificationService);
        var request = new DeleteDocumentDirectoryRequest {
            DocumentDirectoryID = DocumentDirectoryID
        };

        // act
        var response = await sut.HandleAsync(request, CancellationToken.None);

        // assert
        response.Match(
            onSuccess: _ => {
                Assert.Multiple(() => {
                    Assert.NotEqual(Guid.Empty, response.Value.DocumentDirectoryID);
                    Assert.NotEmpty(notifications);
                    Assert.Equal(NotificationType.Success, notifications[0].Type);
                });
            },
            onFailure: errors => Assert.Fail($"Expected success but got failure: {errors.ToJson()}")
        );
    }

    private static async Task CreateDocumentDirectoryAsync(AppDbContext dbContext, bool state, CancellationToken cancellationToken) {
        var documentDirectory = new DocumentDirectory {
            ID = DocumentDirectoryID,
            Label = nameof(DocumentDirectory),
            DirectoryPath = typeof(DeleteDocumentDirectoryRequestHandlerTests).Assembly.GetDirectoryPath(),
            DisplayPosition = 1
        };

        await dbContext.DocumentDirectories.AddAsync(documentDirectory, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

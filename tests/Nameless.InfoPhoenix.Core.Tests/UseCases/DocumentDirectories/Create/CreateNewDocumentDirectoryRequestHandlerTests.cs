using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Testing.Tools;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Core;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Notifications;
using Nameless.Testing.Tools.Attributes;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

[UnitTest]
public class CreateNewDocumentDirectoryRequestHandlerTests {
    private static CreateNewDocumentDirectoryRequestHandler CreateSut(
        AppDbContext appDbContext,
        INotificationService notificationService = null,
        IValidationService validationService = null,
        ILogger<CreateNewDocumentDirectoryRequestHandler> logger = null) {

        notificationService ??= new NotificationServiceMocker().Build();
        logger ??= NullLogger<CreateNewDocumentDirectoryRequestHandler>.Instance;
        validationService ??=  new ValidationServiceMocker().WithValidateAsync(ValidationResult.Success())
                                                            .Build();

        return new CreateNewDocumentDirectoryRequestHandler(
            appDbContext,
            notificationService,
            validationService,
            logger
        );
    }

    [Fact]
    public async Task Happy_Path() {
        // arrange
        var notifications = new List<CreateNewDocumentDirectoryNotification>();
        var notificationService = new NotificationServiceMocker().WithPublishAsync<CreateNewDocumentDirectoryNotification>(notifications.Add)
                                                                 .Build();
        await using var appDbContext = await AppDbContextFactory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var sut = CreateSut(appDbContext, notificationService);
        var request = new CreateNewDocumentDirectoryRequest {
            Label = nameof(CreateNewDocumentDirectoryRequestHandlerTests),
            DirectoryPath = typeof(CreateNewDocumentDirectoryRequestHandlerTests).Assembly.GetDirectoryPath(),
            DisplayPosition = 1
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
}

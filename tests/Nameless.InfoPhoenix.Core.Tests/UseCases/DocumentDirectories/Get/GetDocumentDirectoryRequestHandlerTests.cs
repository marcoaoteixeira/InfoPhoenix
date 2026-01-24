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

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Get;

[UnitTest]
public class GetDocumentDirectoryRequestHandlerTests {
    private static readonly Guid DocumentDirectoryID = Guid.NewGuid();

    private static GetDocumentDirectoryRequestHandler CreateSut(
        AppDbContext appDbContext,
        INotificationService notificationService = null,
        IValidationService validationService = null,
        ILogger<GetDocumentDirectoryRequestHandler> logger = null) {

        notificationService ??= new NotificationServiceMocker().Build();
        validationService ??= new ValidationServiceMocker().WithValidateAsync(ValidationResult.Success())
                                                           .Build();
        logger ??= NullLogger<GetDocumentDirectoryRequestHandler>.Instance;

        return new GetDocumentDirectoryRequestHandler(
            appDbContext,
            notificationService,
            validationService,
            logger
        );
    }

    [Fact]
    public async Task Happy_Path() {
        // arrange
        await using var appDbContext = await AppDbContextFactory.CreateAsync(opts => {
            opts.UseSeeding(CreateDocumentDirectoryAsync);
        }, TestContext.Current.CancellationToken);

        var sut = CreateSut(appDbContext);
        var request = new GetDocumentDirectoryRequest {
            DocumentDirectoryID = DocumentDirectoryID
        };

        // act
        var response = await sut.HandleAsync(request, CancellationToken.None);

        // assert
        response.Match(
            onSuccess: _ => Assert.Equal(nameof(DocumentDirectory), response.Value.Label),
            onFailure: errors => Assert.Fail($"Expected success but got failure: {errors.ToJson()}")
        );
    }

    private static async Task CreateDocumentDirectoryAsync(AppDbContext dbContext, bool state, CancellationToken cancellationToken) {
        var documentDirectory = new DocumentDirectory {
            ID = DocumentDirectoryID,
            Label = nameof(DocumentDirectory),
            DirectoryPath = typeof(GetDocumentDirectoryRequestHandlerTests).Assembly.GetDirectoryPath(),
            DisplayPosition = 1
        };

        await dbContext.DocumentDirectories.AddAsync(documentDirectory, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

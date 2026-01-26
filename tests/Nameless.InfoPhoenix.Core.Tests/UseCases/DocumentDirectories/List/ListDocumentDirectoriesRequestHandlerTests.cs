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

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.List;

[UnitTest]
public class ListDocumentDirectoriesRequestHandlerTests {
    private static ListDocumentDirectoriesRequestHandler CreateSut(
        AppDbContext appDbContext,
        INotificationService notificationService = null,
        IValidationService validationService = null,
        ILogger<ListDocumentDirectoriesRequestHandler> logger = null) {
        notificationService ??= new NotificationServiceMocker().Build();
        validationService ??= new ValidationServiceMocker().WithValidateAsync(ValidationResult.Create())
                                                           .Build();
        logger ??= NullLogger<ListDocumentDirectoriesRequestHandler>.Instance;

        return new ListDocumentDirectoriesRequestHandler(
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
        var request = new ListDocumentDirectoriesRequest();

        // act
        var response = await sut.HandleAsync(request, CancellationToken.None);

        // assert
        response.Match(
            onSuccess: _ => Assert.Multiple(() => {
                Assert.NotEmpty(response.Value);
                Assert.Equal(2, response.Value.Length);
            }),
            onFailure: errors => Assert.Fail($"Expected success but got failure: {errors.ToJson()}")
        );
    }

    private static async Task CreateDocumentDirectoryAsync(AppDbContext dbContext, bool state, CancellationToken cancellationToken) {
        var documentDirectory1 = new DocumentDirectory {
            ID = Guid.NewGuid(),
            Label = $"{nameof(DocumentDirectory)} 1",
            DirectoryPath = typeof(ListDocumentDirectoriesRequestHandlerTests).Assembly.GetDirectoryPath(),
            DisplayPosition = 1
        };
        await dbContext.DocumentDirectories.AddAsync(documentDirectory1, cancellationToken);

        var documentDirectory2 = new DocumentDirectory {
            ID = Guid.NewGuid(),
            Label = $"{nameof(DocumentDirectory)} 2",
            DirectoryPath = typeof(ListDocumentDirectoriesRequestHandlerTests).Assembly.GetDirectoryPath(),
            DisplayPosition = 2
        };
        await dbContext.DocumentDirectories.AddAsync(documentDirectory2, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

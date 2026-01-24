using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Testing.Tools;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Core;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Documents;
using Nameless.InfoPhoenix.Testing.Tools.Mockers.Notifications;
using Nameless.IO.FileSystem;
using Nameless.Testing.Tools;
using Nameless.Testing.Tools.Attributes;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

[UnitTest]
public class FetchDocumentsForDocumentDirectoryRequestHandlerTests {
    private static FetchDocumentsForDocumentDirectoryRequestHandler CreateSut(
        AppDbContext appDbContext,
        IDocumentService documentReaderManager = null,
        IFileSystem fileSystem = null,
        INotificationService notificationService = null,
        IValidationService validationService = null,
        ILogger<FetchDocumentsForDocumentDirectoryRequestHandler> logger = null) {

        notificationService ??= new NotificationServiceMocker().Build();
        documentReaderManager ??= new DocumentReaderManagerMocker().Build();
        fileSystem ??= new FileSystemImpl(OptionsHelper.Create<FileSystemOptions>(opts => {
            opts.AllowOperationOutsideRoot = true;
            opts.Root = StaticData.ResourcesDirectoryPath;
        }));
        validationService ??= new ValidationServiceMocker().WithValidateAsync(ValidationResult.Success())
                                                           .Build();
        logger ??= NullLogger<FetchDocumentsForDocumentDirectoryRequestHandler>.Instance;

        return new FetchDocumentsForDocumentDirectoryRequestHandler(
            appDbContext,
            notificationService,
            validationService,
            fileSystem,
            documentReaderManager,
            logger
        );
    }

    [Fact]
    public async Task WhenFetchingDocuments_Happy_Path() {
        // arrange
        await using var appDbContext = await AppDbContextFactory.CreateAsync(opts => {
            opts.UseSeeding(StaticData.SeedingAsync);
        }, TestContext.Current.CancellationToken);

        var documentReaderManager = new DocumentReaderManagerMocker().WithGetContentAsync("This is a test")
                                                                     .Build();
        var sut = CreateSut(appDbContext, documentReaderManager);
        var request = new FetchDocumentsForDocumentDirectoryRequest {
            DocumentDirectoryID = StaticData.GetSampleDocumentDirectories().First().ID
        };

        // act
        var response = await sut.HandleAsync(request, CancellationToken.None);

        // assert
        response.Match(
            onSuccess: _ => Assert.Equal(1, response.Value.DocumentCount),
            onFailure: errors => Assert.Fail($"Expected success but got failure: {errors.ToJson()}")
        );
    }
}

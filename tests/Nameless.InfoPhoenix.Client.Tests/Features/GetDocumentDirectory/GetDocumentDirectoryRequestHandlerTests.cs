using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.Features.GetDocumentDirectory;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.Windows.Notification.SnackBar;

namespace Nameless.InfoPhoenix.Client.Tests.Features.GetDocumentDirectory;

public sealed class GetDocumentDirectoryRequestHandlerTests : IDisposable {
    private readonly string _databasePath = Path.Combine(
        Directory.CreateTempSubdirectory("ipx-0015-get-document-directory-").FullName, "app.db");

    public void Dispose() {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Path.GetDirectoryName(_databasePath)!, recursive: true);
    }

    /// <summary>
    /// B18 (plan IPX-0015 D2 table): the dynamic key composes to
    /// <c>GetDocumentDirectoryRequestHandler_HandleAsync_MissingDocumentDirectory</c> with the
    /// requested ID passed as a format argument, not folded into the key itself.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithAnUnknownID_PublishesTheComposedKey_WithTheIDAsAFormatArgument() {
        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
        var eventPublisher = new FakeEventPublisher();
        var translator = new FakeTranslator();
        var handler = new GetDocumentDirectoryRequestHandler(
            dbContext, eventPublisher, translator.For<GetDocumentDirectoryRequestHandler>(),
            NullLogger<GetDocumentDirectoryRequestHandler>.Instance);
        var missingID = Guid.NewGuid();

        var result = await handler.HandleAsync(new GetDocumentDirectoryRequest { ID = missingID }, CancellationToken.None);

        Assert.Equal(Nameless.InfoPhoenix.Client.Data.DTOs.DocumentDirectoryDto.Empty, result);
        var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
        Assert.Equal("GetDocumentDirectoryRequestHandler_HandleAsync_MissingDocumentDirectory", notification.Message);

        var request = Assert.Single(
            translator.Requests, r => r.Key == "GetDocumentDirectoryRequestHandler_HandleAsync_MissingDocumentDirectory");
        Assert.Equal([missingID], request.Args);
    }

    private AppDbContext CreateDbContext() {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_databasePath}").Options;

        return new AppDbContext(options);
    }
}
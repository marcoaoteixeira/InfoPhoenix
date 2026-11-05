using System.IO;
using Nameless.InfoPhoenix.Client.Features.OpenCurrentLogFile;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.IO.Physical;
using Nameless.Windows.Notification.SnackBar;

namespace Nameless.InfoPhoenix.Client.Tests.Features.OpenCurrentLogFile;

/// <summary>
/// The handler resolves its logs directory from <see cref="AppContext.BaseDirectory"/> (the
/// same absolute root the Serilog sink uses, plan D4), which under the test runner is the test
/// assembly's own output directory. Each test creates and removes its own
/// <see cref="Constants.Logging.Directory"/> subtree there, never relying on state another
/// test or a previous run left behind.
/// </summary>
public sealed class OpenCurrentLogFileRequestHandlerTests : IDisposable {
    private readonly string _logsDirectory = Path.Combine(AppContext.BaseDirectory, Constants.Logging.Directory);

    public OpenCurrentLogFileRequestHandlerTests() {
        if (Directory.Exists(_logsDirectory)) {
            Directory.Delete(_logsDirectory, recursive: true);
        }
    }

    public void Dispose() {
        if (Directory.Exists(_logsDirectory)) {
            Directory.Delete(_logsDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task HandleAsync_WithNoLogsDirectory_PublishesWarningAndDoesNotThrow() {
        var eventPublisher = new FakeEventPublisher();
        var handler = CreateHandler(eventPublisher);

        var error = await Record.ExceptionAsync(
            () => handler.HandleAsync(new OpenCurrentLogFileRequest(), CancellationToken.None));

        Assert.Null(error);
        var snackBarEvent = Assert.IsType<SnackBarNotificationEvent>(Assert.Single(eventPublisher.PublishedEvents));
        Assert.Equal("OpenCurrentLogFileRequestHandler_Missing_Log_File_Title", snackBarEvent.Title);
        Assert.Equal("OpenCurrentLogFileRequestHandler_Missing_Log_File_Message", snackBarEvent.Message);
    }

    [Fact]
    public void FindCurrentLogFile_WithSeveralRotatedFiles_PicksTheOneWithTheLatestLastWriteTime() {
        // Exercises FindCurrentLogFile directly (not the full HandleAsync), because the real
        // ProcessHelper.OpenFile it would reach for an existing file launches the OS default
        // viewer - a real external process a unit test must not trigger.
        Directory.CreateDirectory(_logsDirectory);

        WriteLogFile("app-20260101.log", DateTime.UtcNow.AddDays(-2));
        var newest = WriteLogFile("app-20260103.log", DateTime.UtcNow);
        WriteLogFile("app-20260102.log", DateTime.UtcNow.AddDays(-1));

        var logDirectory = new PhysicalFileManagerFactory()
            .GetOrCreate(AppContext.BaseDirectory)
            .GetDirectory(Constants.Logging.Directory);

        var currentFile = OpenCurrentLogFileRequestHandler.FindCurrentLogFile(logDirectory);

        Assert.NotNull(currentFile);
        Assert.Equal(newest, currentFile.Path);
    }

    [Fact]
    public void FindCurrentLogFile_WithNoLogsDirectory_ReturnsNull() {
        var logDirectory = new PhysicalFileManagerFactory()
            .GetOrCreate(AppContext.BaseDirectory)
            .GetDirectory(Constants.Logging.Directory);

        var currentFile = OpenCurrentLogFileRequestHandler.FindCurrentLogFile(logDirectory);

        Assert.Null(currentFile);
    }

    private static OpenCurrentLogFileRequestHandler CreateHandler(FakeEventPublisher eventPublisher) {
        return new OpenCurrentLogFileRequestHandler(
            eventPublisher,
            new PhysicalFileManagerFactory(),
            new FakeTranslator()
        );
    }

    private string WriteLogFile(string name, DateTime lastWriteTimeUtc) {
        var path = Path.Combine(_logsDirectory, name);

        File.WriteAllText(path, "log");
        File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

        return path;
    }
}

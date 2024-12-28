using Microsoft.Extensions.Logging;
using Moq;
using Nameless.Application;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Domains.Mockers;
using Nameless.InfoPhoenix.Notification;
using Nameless.Test.Utils;
using Nameless.Test.Utils.Mockers;

namespace Nameless.InfoPhoenix.Domains.UseCases.Database.PerformBackup;

public class PerformDatabaseBackupRequestHandlerTests {
    private static readonly DateTime FileTimestamp = new(year: 2000, month: 1, day: 1, hour: 20, minute: 30, second: 40);

    [Test]
    [Ignore("Need fixing")]
    public async Task WhenCalled_ThenExecuteDatabaseFileBackup() {
        // arrange
        var applicationContextMocker = CreateApplicationContextMocker();
        var clockMocker = CreateClockMocker();
        var fileSystemMocker = CreateFileSystemMocker();
        var loggerMocker = CreateLoggerMocker();

        var sut = CreateSut(applicationContextMocker.Build(),
                            clockMocker.Build(),
                            fileSystemMocker.Build(),
                            Mock.Of<INotificationService>(),
                            loggerMocker.Build());

        var result = await sut.Handle(new PerformDatabaseBackupRequest(), CancellationToken.None);

        Assert.Multiple(() => {
            fileSystemMocker.Verify(mock => mock.Directory.Create(It.IsAny<string>()));
            fileSystemMocker.Verify(mock => mock.File.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));
            fileSystemMocker.Verify(mock => mock.File.Open(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()));
            fileSystemMocker.Verify(mock => mock.File.Create(It.IsAny<string>()));
            fileSystemMocker.Verify(mock => mock.File.Delete(It.IsAny<string>()));

            Assert.That(result.HasErrors, Is.False);
        });
    }

    private static PerformDatabaseBackupRequestHandler CreateSut(IApplicationContext applicationContext,
                                                                 IClock clock,
                                                                 IFileSystem fileSystem,
                                                                 INotificationService notificationService,
                                                                 ILogger<PerformDatabaseBackupRequestHandler> logger)
        => new(applicationContext, clock, fileSystem, notificationService, logger);

    private static LoggerMocker<PerformDatabaseBackupRequestHandler> CreateLoggerMocker()
        => new LoggerMocker<PerformDatabaseBackupRequestHandler>()
            .WithLogLevel(LogLevel.Error,
                          LogLevel.Information,
                          LogLevel.Warning);

    private static FileSystemMocker CreateFileSystemMocker() {
        var fileOperatorMocker = new FileServiceMocker();

        fileOperatorMocker.WithCreate()
                          .WithOpen("This is our fake database file.");

        var fileSystemMocker = new FileSystemMocker().WithFile(fileOperatorMocker.Build());

        return fileSystemMocker;
    }

    private static ClockMocker CreateClockMocker()
        => new ClockMocker().WithUtcNow(FileTimestamp);

    private static ApplicationContextMocker CreateApplicationContextMocker()
        => new ApplicationContextMocker().WithAppDataFolderPath(TestUtilsConstants.BinFolderPath);
}
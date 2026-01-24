using System.IO;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Services;
using Nameless.IO.FileSystem;
using Nameless.Lucene;
using Nameless.Mediator.Requests;
using Nameless.ObjectModel;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public class PerformDocumentIndexBackupRequestHandler : IRequestHandler<PerformDocumentIndexBackupRequest, PerformDocumentIndexBackupResponse> {
    private readonly IFileSystem _fileSystem;
    private readonly IIndexProvider _indexProvider;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly IZipArchiveService _zipArchiveService;
    private readonly ILogger<PerformDocumentIndexBackupRequestHandler> _logger;

    public PerformDocumentIndexBackupRequestHandler(
        IFileSystem fileSystem,
        IIndexProvider indexProvider,
        INotificationService notificationService,
        TimeProvider timeProvider,
        IZipArchiveService zipArchiveService,
        ILogger<PerformDocumentIndexBackupRequestHandler> logger) {
        _fileSystem = fileSystem;
        _indexProvider = indexProvider;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _zipArchiveService = zipArchiveService;
        _logger = logger;
    }

    public async Task<PerformDocumentIndexBackupResponse> HandleAsync(PerformDocumentIndexBackupRequest request, CancellationToken cancellationToken) {
        await _notificationService.NotifyStartingAsync()
                                  .SkipContextSync();

        await _notificationService.NotifyClosingDocumentIndexFinishAsync()
                                  .SkipContextSync();

        // First, dispose the current index to ensure all data is flushed to disk.
        // This is crucial before performing a backup. Also, it will remove the
        // index from the index provider's cache.
        _indexProvider.Get(Constants.Index.Name).Dispose();

        // Next, perform the backup operation.
        var indexDirectory = _fileSystem.GetDirectory(Constants.Index.DirectoryName);
        var backupDirectory = _fileSystem.GetDirectory(Constants.Application.Backup.DirectoryName);
        var backupFileName = string.Format(Constants.Index.Backup.FileNamePattern, _timeProvider.GetUtcNow().UtcDateTime);
        var backupFilePath = Path.Combine(backupDirectory.Path, backupFileName);

        await _notificationService.NotifyCreatingBackupFileAsync()
                                  .SkipContextSync();
        try {
            await _zipArchiveService.CompressDirectoryAsync(
                indexDirectory.Path,
                backupFilePath,
                cancellationToken
            ).SkipContextSync();
        }
        catch (Exception ex) {
            await _notificationService.NotifyFailureAsync(ex.Message)
                                      .SkipContextSync();

            _logger.ExecuteDocumentIndexBackupFailure(ex);

            return Error.Failure(ex.Message);
        }

        await _notificationService.NotifySuccessAsync(backupFilePath)
                                  .SkipContextSync();

        return new PerformDocumentIndexBackupMetadata(backupFilePath);
    }
}
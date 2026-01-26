using System.IO;
using System.IO.Compression;
using Nameless.Compression;
using Nameless.Compression.Requests;
using Nameless.IO.FileSystem;
using Nameless.Lucene;
using Nameless.Mediator.Requests;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public class PerformDocumentIndexBackupRequestHandler : IRequestHandler<PerformDocumentIndexBackupRequest, PerformDocumentIndexBackupResponse> {
    private readonly IFileSystem _fileSystem;
    private readonly IIndexProvider _indexProvider;
    private readonly INotificationService _notificationService;
    private readonly IZipArchiveService _zipArchiveService;

    public PerformDocumentIndexBackupRequestHandler(
        IFileSystem fileSystem,
        IIndexProvider indexProvider,
        INotificationService notificationService,
        IZipArchiveService zipArchiveService) {
        _fileSystem = fileSystem;
        _indexProvider = indexProvider;
        _notificationService = notificationService;
        _zipArchiveService = zipArchiveService;
    }

    public async Task<PerformDocumentIndexBackupResponse> HandleAsync(PerformDocumentIndexBackupRequest request, CancellationToken cancellationToken) {
        await _notificationService.NotifyStartingAsync()
                                  .SkipContextSync();

        // First, dispose the current index to ensure all data is flushed to disk.
        // This is crucial before performing a backup. Also, it will remove the
        // index from the index provider's cache.
        _indexProvider.Get(Constants.Index.Name).Dispose();

        // Next, perform the backup operation.
        var sourceDirectoryPath = GetSourceDirectory();
        var backupFilePath = GetBackupFilePath(request.BackupDate);

        var compressArchiveRequest = new CompressArchiveRequest {
            DestinationFilePath = $"{backupFilePath}{Constants.Application.Backup.FileExtension}",
            CompressionLevel = CompressionLevel.Optimal
        }.IncludeDirectory(sourceDirectoryPath);

        var compressArchiveResponse = await _zipArchiveService.CompressAsync(compressArchiveRequest, cancellationToken)
                                                              .SkipContextSync();

        if (!compressArchiveResponse.Success) {
            await _notificationService.NotifyFailureAsync(compressArchiveResponse.Errors[0].Message)
                                      .SkipContextSync();

            return compressArchiveResponse.Errors[0];
        }

        await _notificationService.NotifySuccessAsync(compressArchiveResponse.Value.FilePath)
                                  .SkipContextSync();

        return new PerformDocumentIndexBackupMetadata(compressArchiveResponse.Value.FilePath);

        string GetSourceDirectory() {
            return _fileSystem.GetFullPath(
                Path.Combine(Constants.Index.DirectoryName, Constants.Index.Name)
            );
        }

        string GetBackupFilePath(DateTimeOffset backupDate) {
            var backupFileName = string.Format(
                Constants.Index.Backup.FileNamePattern,
                backupDate
            );

            return _fileSystem.GetFullPath(
                Path.Combine(
                    Constants.Index.DirectoryName,
                    backupFileName
                )
            );
        }
    }
}
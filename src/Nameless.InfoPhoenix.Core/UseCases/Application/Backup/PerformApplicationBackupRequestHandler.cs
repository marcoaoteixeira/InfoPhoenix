using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Nameless.Compression;
using Nameless.Compression.Requests;
using Nameless.InfoPhoenix.UseCases.Database.Backup;
using Nameless.InfoPhoenix.UseCases.Index.Backup;
using Nameless.IO.FileSystem;
using Nameless.Mediator;
using Nameless.Mediator.Requests;
using Nameless.ObjectModel;
using Nameless.Results;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Application.Backup;

public class PerformApplicationBackupRequestHandler : IRequestHandler<PerformApplicationBackupRequest, PerformApplicationBackupResponse> {
    private readonly IFileSystem _fileSystem;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly IZipArchiveService _zipArchiveService;
    private readonly ILogger<PerformApplicationBackupRequestHandler> _logger;

    public PerformApplicationBackupRequestHandler(
        IFileSystem fileSystem,
        IMediator mediator,
        INotificationService notificationService,
        TimeProvider timeProvider,
        IZipArchiveService zipArchiveService,
        ILogger<PerformApplicationBackupRequestHandler> logger) {
        _fileSystem = fileSystem;
        _mediator = mediator;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _zipArchiveService = zipArchiveService;
        _logger = logger;
    }

    public async Task<PerformApplicationBackupResponse> HandleAsync(PerformApplicationBackupRequest request, CancellationToken cancellationToken) {
        EnsureApplicationBackupDirectoryExistence();

        await _notificationService.NotifyApplicationBackupStartingAsync()
                                  .SkipContextSync();

        var backupDate = _timeProvider.GetUtcNow();
        var databaseTask = _mediator.ExecuteAsync(
            new PerformDatabaseBackupRequest { BackupDate = backupDate },
            cancellationToken
        );
        var documentIndexTask = _mediator.ExecuteAsync(
            new PerformDocumentIndexBackupRequest { BackupDate = backupDate },
            cancellationToken
        );

        await Task.WhenAll(databaseTask, documentIndexTask)
                  .SkipContextSync();

        var databaseResponse = await databaseTask;
        var documentIndexResponse = await documentIndexTask;

        if (!databaseResponse.Success || !documentIndexResponse.Success) {
            await _notificationService.NotifyApplicationBackupFailureAsync()
                                      .SkipContextSync();

            Error[] result = [
                ..GetErrors(databaseResponse),
                ..GetErrors(documentIndexResponse)
            ];

            return result;
        }

        // Here we combine the backup files into a single application backup file.
        var databaseBackupFile = databaseResponse.Value.BackupFilePath;
        var documentIndexBackupFile = documentIndexResponse.Value.BackupFilePath;

        var backupFilePath = GetBackupFilePath();
        var compressArchiveRequest = new CompressArchiveRequest {
            DestinationFilePath = $"{backupFilePath}{Constants.Application.Backup.FileExtension}",
            CompressionLevel = CompressionLevel.NoCompression
        };

        compressArchiveRequest.IncludeFile(databaseBackupFile)
                              .IncludeFile(documentIndexBackupFile);

        var compressArchiveResponse = await _zipArchiveService.CompressAsync(compressArchiveRequest, cancellationToken)
                                                              .SkipContextSync();

        if (!compressArchiveResponse.Success) {
            _logger.ExecuteApplicationBackupFailure(
                compressArchiveResponse.Errors[0].Message
            );

            await _notificationService.NotifyApplicationBackupFailureAsync()
                                      .SkipContextSync();

            return compressArchiveResponse.Errors;
        }

        await CleanUpAsync();

        await _notificationService.NotifyApplicationBackupSuccessAsync(compressArchiveResponse.Value.FilePath)
                                  .SkipContextSync();

        return new PerformApplicationBackupMetadata(
            BackupFilePath: compressArchiveResponse.Value.FilePath
        );

        void EnsureApplicationBackupDirectoryExistence() {
            _fileSystem.GetDirectory(Constants.Application.Backup.DirectoryName)
                       .Create();
        }

        string GetBackupFilePath() {
            var backupFileName = string.Format(
                Constants.Application.Backup.FileNamePattern,
                backupDate
            );

            return _fileSystem.GetFullPath(
                Path.Combine(
                    Constants.Application.Backup.DirectoryName,
                    backupFileName
                )
            );
        }

        static Error[] GetErrors<T>(Result<T> result) {
            return !result.Success ? result.Errors : [];
        }

        async Task CleanUpAsync() {
            await _notificationService.NotifyApplicationBackupCleanUpAsync()
                                      .SkipContextSync();

            _fileSystem.GetFile(databaseBackupFile).Delete();
            _fileSystem.GetFile(documentIndexBackupFile).Delete();
        }
    }
}
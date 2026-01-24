using System.IO;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nameless.Compression;
using Nameless.Compression.Requests;
using Nameless.IO.FileSystem;
using Nameless.Mediator.Requests;
using Nameless.ObjectModel;
using Nameless.Results;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

public class PerformDatabaseBackupRequestHandler : IRequestHandler<PerformDatabaseBackupRequest, PerformDatabaseBackupResponse> {
    private readonly IFileSystem _fileSystem;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly IZipArchiveService _zipArchiveService;
    private readonly ILogger<PerformDatabaseBackupRequestHandler> _logger;

    public PerformDatabaseBackupRequestHandler(
        IFileSystem fileSystem,
        INotificationService notificationService,
        TimeProvider timeProvider,
        IZipArchiveService zipArchiveService,
        ILogger<PerformDatabaseBackupRequestHandler> logger) {
        _fileSystem = fileSystem;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _zipArchiveService = zipArchiveService;
        _logger = logger;
    }

    public async Task<PerformDatabaseBackupResponse> HandleAsync(PerformDatabaseBackupRequest request, CancellationToken cancellationToken) {
        EnsureApplicationBackupDirectory();

        var databaseDataBackupResult = await ExecuteDatabaseBackupAsync(cancellationToken);

        if (!databaseDataBackupResult.Success) {
            return databaseDataBackupResult.Errors[0];
        }

        var backupFilePath = databaseDataBackupResult.Value;
        var prepareBackupFileResult = await PrepareBackupFileAsync(
            backupFilePath,
            cancellationToken
        ).SkipContextSync();

        return await prepareBackupFileResult.Match(
            onSuccess: OnSuccess,
            onFailure: errors => OnFailure(errors[0])
        );
    }

    private void EnsureApplicationBackupDirectory() {
        _fileSystem.GetDirectory(Constants.Application.Backup.DirectoryName).Create();
    }

    private async Task<Result<string>> ExecuteDatabaseBackupAsync(CancellationToken cancellationToken) {
        SqliteConnection? backupDbConnection = null;
        SqliteConnection? sourceDbConnection = null;

        try {
            await _notificationService.NotifyDatabaseBackupStartingAsync()
                                      .SkipContextSync();

            var sourceFilePath = GetSourceFilePath();
            sourceDbConnection = await CreateSqliteConnectionAsync(sourceFilePath, cancellationToken).SkipContextSync();

            var backupFilePath = GetBackupFilePath();
            backupDbConnection = await CreateSqliteConnectionAsync(backupFilePath, cancellationToken).SkipContextSync();

            // Executes the backup
            sourceDbConnection.BackupDatabase(backupDbConnection);

            await _notificationService.NotifyDatabaseBackupFinishAsync()
                                      .SkipContextSync();

            return backupFilePath;
        }
        catch (Exception ex) {
            _logger.ExecuteDatabaseBackupFailure(ex);

            await _notificationService.NotifyDatabaseBackupFailureAsync(ex.Message)
                                      .SkipContextSync();

            return Error.Failure(ex.Message);
        }
        finally {
            if (backupDbConnection is not null) {
                await backupDbConnection.CloseAsync().SkipContextSync();
                await backupDbConnection.DisposeAsync();
            }

            if (sourceDbConnection is not null) {
                await sourceDbConnection.CloseAsync().SkipContextSync();
                await sourceDbConnection.DisposeAsync();
            }
        }
    }

    private string GetSourceFilePath() {
        return _fileSystem.GetFullPath(
            Path.Combine(
                Constants.Database.DirectoryName,
                Constants.Database.Name
            )
        );
    }

    private static async Task<SqliteConnection> CreateSqliteConnectionAsync(string filePath, CancellationToken cancellationToken) {
        var connStr = string.Format(Constants.Database.ConnStrPattern, filePath);
        var dbConnection = new SqliteConnection(connStr);

        await dbConnection.OpenAsync(cancellationToken)
                          .SkipContextSync();

        return dbConnection;
    }

    private string GetBackupFilePath() {
        var now = _timeProvider.GetUtcNow();
        var backupFileName = string.Format(
            Constants.Database.Backup.FileNamePattern,
            now
        );

        return _fileSystem.GetFullPath(
            Path.Combine(
                Constants.Application.Backup.DirectoryName,
                backupFileName
            )
        );
    }

    private async Task<Result<string>> PrepareBackupFileAsync(string backupFilePath, CancellationToken cancellationToken) {
        try {
            await _notificationService.NotifyPrepareBackupFileStartingAsync()
                                      .SkipContextSync();

            var compressArchiveRequest = new CompressArchiveRequest {
                DestinationFilePath = $"{backupFilePath}{Constants.Application.Backup.FileExtension}",
                CompressionLevel = CompressionLevel.Optimal
            }.IncludeFile(backupFilePath);

            var compressArchiveResponse = await _zipArchiveService.CompressAsync(compressArchiveRequest, cancellationToken)
                                                                  .SkipContextSync();

            _fileSystem.GetFile(backupFilePath)
                       .Delete();

            await _notificationService.NotifyPrepareBackupFileFinishAsync()
                                      .SkipContextSync();

            return compressArchiveResponse.Match<Result<string>>(
                onSuccess: value => value.FilePath,
                onFailure: errors => errors
            );
        }
        catch (Exception ex) {
            _logger.PrepareBackupFileFailure(ex);

            await _notificationService.NotifyPrepareBackupFileFailureAsync(ex.Message)
                                      .SkipContextSync();

            return Error.Failure(ex.Message);
        }
    }

    private async Task<PerformDatabaseBackupResponse> OnSuccess(string backupFilePath) {
        await _notificationService.NotifySuccessAsync(backupFilePath)
                                  .SkipContextSync();

        return new PerformDatabaseBackupMetadata(backupFilePath);
    }

    private static Task<PerformDatabaseBackupResponse> OnFailure(Error error) {
        return Task.FromResult<PerformDatabaseBackupResponse>(error);
    }
}
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
    private readonly IZipArchiveService _zipArchiveService;
    private readonly ILogger<PerformDatabaseBackupRequestHandler> _logger;

    public PerformDatabaseBackupRequestHandler(
        IFileSystem fileSystem,
        INotificationService notificationService,
        IZipArchiveService zipArchiveService,
        ILogger<PerformDatabaseBackupRequestHandler> logger) {
        _fileSystem = fileSystem;
        _notificationService = notificationService;
        _zipArchiveService = zipArchiveService;
        _logger = logger;
    }

    public async Task<PerformDatabaseBackupResponse> HandleAsync(PerformDatabaseBackupRequest request, CancellationToken cancellationToken) {
        await _notificationService.NotifyStartingAsync()
                                  .SkipContextSync();

        var databaseDataBackupResult = await ExecuteDatabaseBackupAsync(
            request.BackupDate,
            cancellationToken
        ).SkipContextSync();

        if (!databaseDataBackupResult.Success) {
            return databaseDataBackupResult.Errors[0];
        }

        var prepareBackupFileResult = await PrepareBackupFileAsync(
            databaseDataBackupResult.Value,
            cancellationToken
        ).SkipContextSync();

        if (!prepareBackupFileResult.Success) {
            return prepareBackupFileResult.Errors[0];
        }

        await _notificationService.NotifySuccessAsync(
            prepareBackupFileResult.Value
        ).SkipContextSync();

        return new PerformDatabaseBackupMetadata(
            BackupFilePath: prepareBackupFileResult.Value
        );
    }

    private async Task<Result<string>> ExecuteDatabaseBackupAsync(DateTimeOffset backupDate, CancellationToken cancellationToken) {
        SqliteConnection? backupDbConnection = null;
        SqliteConnection? sourceDbConnection = null;

        try {
            var sourceFilePath = GetSourceFilePath();
            sourceDbConnection = await CreateSqliteConnectionAsync(sourceFilePath, cancellationToken).SkipContextSync();

            var backupFilePath = GetBackupFilePath();
            backupDbConnection = await CreateSqliteConnectionAsync(backupFilePath, cancellationToken).SkipContextSync();

            sourceDbConnection.BackupDatabase(backupDbConnection);

            return backupFilePath;
        }
        catch (Exception ex) {
            _logger.ExecuteDatabaseBackupFailure(ex);

            await _notificationService.NotifyFailureAsync(ex.Message)
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

        string GetSourceFilePath() {
            return _fileSystem.GetFullPath(
                Path.Combine(
                    Constants.Database.DirectoryName,
                    Constants.Database.Name
                )
            );
        }

        static async Task<SqliteConnection> CreateSqliteConnectionAsync(string filePath, CancellationToken cancellationToken) {
            var connStr = string.Format(Constants.Database.ConnStrPattern, filePath);
            var dbConnection = new SqliteConnection(connStr);

            await dbConnection.OpenAsync(cancellationToken)
                              .SkipContextSync();

            return dbConnection;
        }

        string GetBackupFilePath() {
            var backupFileName = string.Format(
                Constants.Database.Backup.FileNamePattern,
                backupDate
            );

            return _fileSystem.GetFullPath(
                Path.Combine(
                    Constants.Database.DirectoryName,
                    backupFileName
                )
            );
        }
    }

    private async Task<Result<string>> PrepareBackupFileAsync(string backupFilePath, CancellationToken cancellationToken) {
        var compressArchiveRequest = new CompressArchiveRequest {
            DestinationFilePath = $"{backupFilePath}{Constants.Application.Backup.FileExtension}",
            CompressionLevel = CompressionLevel.Optimal
        }.IncludeFile(backupFilePath);

        var compressArchiveResponse = await _zipArchiveService.CompressAsync(
            compressArchiveRequest,
            cancellationToken
        ).SkipContextSync();

        if (!compressArchiveResponse.Success) {
            await _notificationService.NotifyFailureAsync(
                compressArchiveResponse.Errors[0].Message
            ).SkipContextSync();

            return compressArchiveResponse.Errors[0];
        }

        _fileSystem.GetFile(backupFilePath)
                   .Delete();

        return compressArchiveResponse.Value.FilePath;
    }
}
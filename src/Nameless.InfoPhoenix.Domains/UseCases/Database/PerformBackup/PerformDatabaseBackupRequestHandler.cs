using System.IO.Compression;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nameless.Application;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Notification;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Domains.UseCases.Database.PerformBackup;

public sealed class PerformDatabaseBackupRequestHandler : IRequestHandler<PerformDatabaseBackupRequest, Result<string>> {
    private readonly IApplicationContext _applicationContext;
    private readonly IClock _clock;
    private readonly IFileSystem _fileSystem;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PerformDatabaseBackupRequestHandler> _logger;

    public PerformDatabaseBackupRequestHandler(IApplicationContext applicationContext,
                                               IClock clock,
                                               IFileSystem fileSystem,
                                               INotificationService notificationService,
                                               ILogger<PerformDatabaseBackupRequestHandler> logger) {
        _applicationContext = Prevent.Argument.Null(applicationContext);
        _clock = Prevent.Argument.Null(clock);
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _notificationService = Prevent.Argument.Null(notificationService);
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task<Result<string>> Handle(PerformDatabaseBackupRequest request, CancellationToken cancellationToken) {
        try {
            var result = await ExecuteDatabaseBackupAsync(cancellationToken);

            if (result.HasErrors) {
                return result;
            }

            var backupFilePath = result.Value;

            await CreateBackupFileAsync(backupFilePath, cancellationToken);

            CleanBackupIntermediateFiles(backupFilePath);

            await _notificationService.PublishAsync(new DatabaseBackupPerformedNotification {
                Title = "Backup de Base de Dados",
                Message = "Backup realizado com sucesso. Arquivo poderá ser encontrado na pasta de backups dentro do diretório de dados da aplicação.",
                Type = NotificationType.Success
            });

            return result;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "An error occurred while backup application database.");

            await _notificationService.PublishAsync(new DatabaseBackupPerformedNotification {
                Title = "Backup de Base de Dados",
                Message = $"Ocorreu um erro ao tentar realizar o backup da base de dados. Erro: {ex.Message}",
                Type = NotificationType.Error
            });

            return Error.Failure(ex.Message);
        }
    }

    private void CleanBackupIntermediateFiles(string backupFilePath)
        => _fileSystem.File.Delete(backupFilePath);

    private async Task CreateBackupFileAsync(string backupFilePath, CancellationToken cancellationToken) {
        await using var backupFileStream = _fileSystem.File.Open(backupFilePath,
                                                                 FileMode.Open,
                                                                 FileAccess.Read,
                                                                 FileShare.Inheritable);
        await using var compressFileStream = _fileSystem.File.Create($"{backupFilePath}.gz");
        await using var gzipStream = new GZipStream(compressFileStream, CompressionLevel.Optimal);

        await backupFileStream.CopyToAsync(gzipStream, cancellationToken);
    }

    private async Task<Result<string>> ExecuteDatabaseBackupAsync(CancellationToken cancellationToken) {
        SqliteConnection? backupDbConnection = null;
        SqliteConnection? sourceDbConnection = null;
        try {
            var sourceFilePath = GetSourceFilePath();
            sourceDbConnection = await CreateSqliteConnection(sourceFilePath, cancellationToken);

            var backupFilePath = GetBackupFilePath();
            backupDbConnection = await CreateSqliteConnection(backupFilePath, cancellationToken);

            sourceDbConnection.BackupDatabase(backupDbConnection);

            return backupFilePath;
        }
        catch (Exception ex) { return Error.Failure(ex.Message); }
        finally {
            if (backupDbConnection is not null) {
                await backupDbConnection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
                await backupDbConnection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
            }

            if (sourceDbConnection is not null) {
                await sourceDbConnection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
                await sourceDbConnection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }

    private static async Task<SqliteConnection> CreateSqliteConnection(string filePath, CancellationToken cancellationToken) {
        var connStr = string.Format(Constants.Common.ConnStrPattern, filePath);
        var dbConnection = new SqliteConnection(connStr);

        await dbConnection.OpenAsync(cancellationToken);

        return dbConnection;
    }

    private string GetBackupFilePath() {
        var backupFolderPath = Path.Combine(_applicationContext.AppDataFolderPath,
                                            Constants.Common.DatabaseBackupFolderName);

        // Ensure directory exists
        _fileSystem.Directory.Create(backupFolderPath);

        var backupFileName = string.Format(Constants.Common.DatabaseBackupFileNamePattern,
                                           _clock.GetUtcNow());

        return Path.Combine(backupFolderPath, backupFileName);
    }

    private string GetSourceFilePath()
        => Path.Combine(_applicationContext.AppDataFolderPath, Constants.Common.DatabaseFileName);
}
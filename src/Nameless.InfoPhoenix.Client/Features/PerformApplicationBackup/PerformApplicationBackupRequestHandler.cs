using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nameless.Application;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.IO;
using Nameless.Lucene;
using Nameless.Mediator.Requests;
using Nameless.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Events;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.BackupRecovery;

namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;

/// <summary>
/// Creates a local backup archive (plan IPX-0009): settings, database and search index, staged
/// in a per-run temporary folder and moved into <c>backups\</c> in one atomic rename, so that
/// folder never holds a partial file (AC2/AC3).
/// </summary>
/// <remarks>
/// Follows the IPX-0008 handler contract verbatim: one Information entry per step, exactly one
/// Success or one Failure, cancellation propagates unreported, and every <c>await</c> resumes on
/// the calling (UI) context - no <c>ConfigureAwait(false)</c> anywhere in this type.
/// </remarks>
public sealed class PerformApplicationBackupRequestHandler
    : IRequestHandler<PerformApplicationBackupRequest, PerformApplicationBackupResult> {
    private const string Prefix = nameof(PerformApplicationBackupRequestHandler);

    private static readonly JsonSerializerOptions ManifestSerializerOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true
    };

    private readonly IApplicationContext _applicationContext;
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IIndexProvider _indexProvider;
    private readonly IFileManager _fileManager;
    private readonly LuceneOptions _luceneOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly ITranslator _translator;
    private readonly ILogger<PerformApplicationBackupRequestHandler> _logger;

    public PerformApplicationBackupRequestHandler(
        IApplicationContext applicationContext,
        IAppConfigurationManager appConfigurationManager,
        IIndexProvider indexProvider,
        IFileManager fileManager,
        IOptions<LuceneOptions> luceneOptions,
        TimeProvider timeProvider,
        IEventPublisher eventPublisher,
        ITranslator translator,
        ILogger<PerformApplicationBackupRequestHandler> logger) {
        _applicationContext = Throws.When.Null(applicationContext);
        _appConfigurationManager = Throws.When.Null(appConfigurationManager);
        _indexProvider = Throws.When.Null(indexProvider);
        _fileManager = Throws.When.Null(fileManager);
        _luceneOptions = Throws.When.Null(luceneOptions).Value;
        _timeProvider = Throws.When.Null(timeProvider);
        _eventPublisher = Throws.When.Null(eventPublisher);
        _translator = Throws.When.Null(translator);
        _logger = Throws.When.Null(logger);
    }

    /// <summary>
    /// Test-only hook (internal, see <c>InternalsVisibleTo</c>) invoked immediately before the
    /// commit-point cancellation check (plan §3.2). That check sits in a window with no
    /// externally observable event either side of it, so no publish-based test fixture can land
    /// a cancel there deterministically; this hook lets a test do so without racing timing.
    /// Null (no-op) in production.
    /// </summary>
    internal Action? OnBeforeCommitPointCheck { get; set; }

    public async Task<PerformApplicationBackupResult> HandleAsync(
        PerformApplicationBackupRequest request, CancellationToken cancellationToken) {
        var step = "Preparing";
        string? stagingDirectory = null;
        string? finalPath = null;

        try {
            await PublishStepAsync(step);

            var (staging, content, timestamp) = await Task.Run(PrepareStaging, cancellationToken);
            stagingDirectory = staging;

            step = "CopyingConfiguration";
            await PublishStepAsync(step);
            await _appConfigurationManager.SaveChangesAsync(cancellationToken);
            var hasConfiguration = await Task.Run(() => CopyConfiguration(content), cancellationToken);
            if (!hasConfiguration) {
                await PublishWarningAsync("ConfigurationMissing");
            }

            step = "CopyingDatabase";
            await PublishStepAsync(step);
            var (stagedDatabasePath, databaseMigrationId) = await Task.Run(() => CopyDatabase(content), cancellationToken);
            if (stagedDatabasePath is null) {
                await PublishWarningAsync("DatabaseMissing");
            }

            step = "CopyingSearchIndex";
            await PublishStepAsync(step);
            var hasSearchIndex = await Task.Run(
                () => {
                    _indexProvider.CloseAll();
                    return CopySearchIndex(content);
                },
                cancellationToken);
            if (!hasSearchIndex) {
                await PublishWarningAsync("SearchIndexMissing");
            }

            step = "CreatingArchive";
            await PublishStepAsync(step);

            var manifest = new BackupManifest(
                FormatVersion: Constants.BackupRecovery.FormatVersion,
                Application: Constants.Application.Name,
                AppVersion: _applicationContext.Version,
                DatabaseMigrationId: databaseMigrationId,
                CreatedAtUtc: timestamp,
                Contents: new BackupManifestContents(
                    Database: stagedDatabasePath is not null,
                    SearchIndex: hasSearchIndex,
                    Configuration: hasConfiguration));

            await WriteManifestAsync(content, manifest, cancellationToken);

            var archivePath = SysPath.Combine(stagingDirectory, "archive.zip");

            // Off the calling (UI) context (finding #2): CreateFromDirectoryAsync still runs its
            // own synchronous enumeration/compression work inline despite the "Async" name.
            await Task.Run(
                () => ZipFile.CreateFromDirectoryAsync(
                    content, archivePath, CompressionLevel.Optimal, includeBaseDirectory: false, cancellationToken: cancellationToken),
                cancellationToken);

            OnBeforeCommitPointCheck?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            // Commit point (plan §3.2): the move below never observes the token, by design -
            // once we are past the check above, a cancel must not interrupt the rename.
            finalPath = await Task.Run(() => MoveToBackupDirectory(archivePath, timestamp));

            await _eventPublisher.PublishSuccessAsync<BackupRecoveryProgressNotificationEvent>(
                message: _translator[$"{Prefix}_Success", [finalPath]]);

            return new PerformApplicationBackupResult(finalPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            if (finalPath is not null) {
                // The move already committed the backup file into backups\ (finding #6) before
                // this failure - e.g. the Success notification publish itself threw - so the
                // operation itself succeeded and must not be downgraded to a failed result.
                _logger.LogError(ex, "Application backup succeeded, but publishing the Success notification failed.");

                return new PerformApplicationBackupResult(finalPath);
            }

            _logger.LogError(ex, "Application backup failed at step '{Step}'.", step);

            await _eventPublisher.PublishFailureAsync<BackupRecoveryProgressNotificationEvent>(
                message: _translator[$"{Prefix}_{step}_Failure", [ex.Message]]);

            return PerformApplicationBackupResult.Failed;
        }
        finally {
            if (stagingDirectory is not null) {
                TryDeleteStagingDirectory(stagingDirectory);
            }
        }
    }

    private Task PublishStepAsync(string step) {
        return _eventPublisher.PublishInformationAsync<BackupRecoveryProgressNotificationEvent>(
            message: _translator[$"{Prefix}_{step}"]);
    }

    private Task PublishWarningAsync(string warningKey) {
        return _eventPublisher.PublishWarningAsync<BackupRecoveryProgressNotificationEvent>(
            message: _translator[$"{Prefix}_{warningKey}_Warning"]);
    }

    /// <summary>
    /// Sweeps any leftover staging folder from a crashed previous run (plan D5 "orphan sweep"),
    /// then creates a fresh one. A GUID name (not the timestamp) avoids a same-second collision
    /// between two runs.
    /// </summary>
    private (string StagingDirectory, string ContentDirectory, DateTimeOffset Timestamp) PrepareStaging() {
        var temporaryDirectory = _applicationContext.FileManager.GetTemporaryDirectory();
        temporaryDirectory.Create();

        SweepOrphanStagingFolders(temporaryDirectory.Path);

        var stagingDirectory = SysPath.Combine(
            temporaryDirectory.Path, Constants.BackupRecovery.StagingDirectoryPrefix + Guid.NewGuid().ToString("N"));
        var contentDirectory = SysPath.Combine(stagingDirectory, "content");

        Directory.CreateDirectory(contentDirectory);

        return (stagingDirectory, contentDirectory, _timeProvider.GetUtcNow());
    }

    private void SweepOrphanStagingFolders(string temporaryDirectoryPath) {
        try {
            foreach (var directory in Directory.EnumerateDirectories(
                temporaryDirectoryPath, $"{Constants.BackupRecovery.StagingDirectoryPrefix}*")) {
                TryDeleteStagingDirectory(directory);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to sweep orphan backup staging folders under '{TemporaryDirectory}'.", temporaryDirectoryPath);
        }
    }

    private void TryDeleteStagingDirectory(string stagingDirectory) {
        try {
            if (Directory.Exists(stagingDirectory)) {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to delete the backup staging folder '{StagingDirectory}'.", stagingDirectory);
        }
    }

    /// <summary>
    /// Flushes the pending in-memory changes (the ViewModel already awaited the IPX-0004 save
    /// chain before this request was sent) and copies <c>app.config</c>. Returns
    /// <see langword="false"/> (D6 "missing artifact is a warning") when the file does not exist.
    /// </summary>
    private bool CopyConfiguration(string contentDirectory) {
        var configurationFile = _applicationContext.FileManager.GetFile(Constants.BackupRecovery.AppConfigurationFileName);

        if (!configurationFile.Exists) {
            return false;
        }

        var destinationPath = SysPath.Combine(contentDirectory, Constants.BackupRecovery.ConfigurationEntryName);

        File.Copy(configurationFile.Path, destinationPath, overwrite: true);

        return true;
    }

    /// <summary>
    /// Copies the SQLite database through the online-backup API (plan D7), never a raw file
    /// copy, so the copy is transactionally consistent even with a live EF connection open.
    /// Forces the staged copy out of WAL mode and reads its migration id on the same connection
    /// before disposing it (finding #1) - a WAL-mode copy would otherwise leak <c>-wal</c>/
    /// <c>-shm</c> sidecar files into the archive, and a separate later connection could race a
    /// checkpoint that has not happened yet. Returns <see langword="null"/>s when there is no
    /// live database (D6).
    /// </summary>
    private (string? Path, string? MigrationId) CopyDatabase(string contentDirectory) {
        var databaseDirectory = _applicationContext.FileManager.GetDatabaseDirectory();
        var sourcePath = SysPath.Combine(databaseDirectory.Path, Constants.Database.FileName);

        if (!File.Exists(sourcePath)) {
            return (null, null);
        }

        var destinationDirectory = SysPath.Combine(contentDirectory, "database");
        Directory.CreateDirectory(destinationDirectory);
        var destinationPath = SysPath.Combine(destinationDirectory, Constants.Database.FileName);

        var sourceConnectionString = new SqliteConnectionStringBuilder {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();
        var destinationConnectionString = new SqliteConnectionStringBuilder {
            DataSource = destinationPath,
            Pooling = false
        }.ToString();

        using var source = new SqliteConnection(sourceConnectionString);
        using var destination = new SqliteConnection(destinationConnectionString);

        source.Open();
        destination.Open();
        source.BackupDatabase(destination);

        using var pragmaCommand = destination.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA journal_mode=DELETE;";
        pragmaCommand.ExecuteNonQuery();

        return (destinationPath, MigrationHistoryReader.ReadLatestMigrationId(destination));
    }

    /// <summary>
    /// Flushes and unlocks the Lucene index (plan D8), then copies it from wherever the DI
    /// <see cref="IFileManager"/>/<see cref="LuceneOptions"/> pair really resolves it (not
    /// <see cref="IApplicationContext.FileManager"/> - see plan R1), skipping <c>write.lock</c>
    /// and any reparse point. Returns <see langword="false"/> (D6) when the index folder does
    /// not exist (today's reality).
    /// </summary>
    private bool CopySearchIndex(string contentDirectory) {
        var sourceDirectory = _fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath);

        if (!sourceDirectory.Exists) {
            return false;
        }

        var destinationDirectory = SysPath.Combine(contentDirectory, Constants.BackupRecovery.SearchIndexEntryDirectory);

        CopyDirectoryRecursive(sourceDirectory.Path, destinationDirectory);

        return true;
    }

    private static void CopyDirectoryRecursive(string sourceDirectory, string destinationDirectory) {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var filePath in Directory.EnumerateFiles(sourceDirectory)) {
            var fileName = SysPath.GetFileName(filePath);

            if (string.Equals(fileName, Constants.BackupRecovery.LuceneWriteLockFileName, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            File.Copy(filePath, SysPath.Combine(destinationDirectory, fileName), overwrite: true);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(sourceDirectory)) {
            // Security (plan §5): never follow a reparse point/junction into the archive.
            if (new DirectoryInfo(directoryPath).Attributes.HasFlag(FileAttributes.ReparsePoint)) {
                continue;
            }

            var directoryName = SysPath.GetFileName(directoryPath);

            CopyDirectoryRecursive(directoryPath, SysPath.Combine(destinationDirectory, directoryName));
        }
    }

    private static async Task WriteManifestAsync(
        string contentDirectory, BackupManifest manifest, CancellationToken cancellationToken) {
        var manifestPath = SysPath.Combine(contentDirectory, Constants.BackupRecovery.ManifestEntryName);
        var json = JsonSerializer.Serialize(manifest, ManifestSerializerOptions);

        await File.WriteAllTextAsync(manifestPath, json, cancellationToken);
    }

    /// <summary>
    /// The commit point (plan §3.2): a non-overwriting move into <c>backups\</c>, retried with a
    /// numeric suffix on a same-second collision (D2/AC3). The staging folder and the backup
    /// folder sit under the same root, so the move is an atomic rename - <c>backups\</c> never
    /// observes a partial file.
    /// </summary>
    private string MoveToBackupDirectory(string archivePath, DateTimeOffset timestamp) {
        var backupDirectory = _applicationContext.FileManager.GetBackupDirectory();
        backupDirectory.Create();

        var baseName = string.Format(
            CultureInfo.InvariantCulture, Constants.BackupRecovery.FileNamePattern, timestamp.UtcDateTime);

        for (var attempt = 1; attempt <= Constants.BackupRecovery.MaxCollisionSuffix; attempt++) {
            var candidateName = attempt == 1
                ? baseName
                : $"{SysPath.GetFileNameWithoutExtension(baseName)}-{attempt}{SysPath.GetExtension(baseName)}";
            var candidatePath = SysPath.Combine(backupDirectory.Path, candidateName);

            try {
                File.Move(archivePath, candidatePath, overwrite: false);

                return candidatePath;
            }
            catch (IOException) when (File.Exists(candidatePath)) {
                // Name taken by a previous run in the same second (D2); try the next suffix.
            }
        }

        throw new IOException(
            $"Could not find an available backup file name after {Constants.BackupRecovery.MaxCollisionSuffix} attempts.");
    }
}
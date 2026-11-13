using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.IO.Physical;
using Nameless.Lucene;
using Nameless.Windows.Events;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.BackupRecovery;

namespace Nameless.InfoPhoenix.Client.Tests.Features.PerformApplicationBackup;

/// <summary>
/// Exercises <see cref="PerformApplicationBackupRequestHandler"/> against two real
/// <c>PhysicalFileManager</c> roots (plan IPX-0009 §6) - one standing in for
/// <see cref="Nameless.Application.IApplicationContext.FileManager"/> (DB/config/backups,
/// "app root" below), and one for the DI-registered <c>IFileManager</c> that resolves the
/// Lucene index (R1's split, "Lucene root" below).
/// </summary>
public sealed class PerformApplicationBackupRequestHandlerTests : IDisposable {
    private readonly string _appRoot = Directory.CreateTempSubdirectory("ipx-0009-app-").FullName;
    private readonly string _luceneRoot = Directory.CreateTempSubdirectory("ipx-0009-lucene-").FullName;

    private const string LuceneRelativePath = "databases/lucene";

    public void Dispose() {
        Directory.Delete(_appRoot, recursive: true);
        Directory.Delete(_luceneRoot, recursive: true);
    }

    /// <summary>B1: all three artifacts present produces a correct archive and manifest.</summary>
    [Fact]
    public async Task HandleAsync_WithAllArtifactsPresent_ProducesACorrectArchiveAndManifest() {
        File.WriteAllText(Path.Combine(_appRoot, "app.config"), """{"Theme":"Dark"}""");
        SeedDatabase(withMigrationHistory: true);
        SeedIndex("segment1.bin", "segment2.bin");

        var indexProvider = new FakeIndexProvider();
        var eventPublisher = new FakeEventPublisher();
        var applicationContext = new FakeApplicationContext {
            Version = "v1.4.0",
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, indexProvider: indexProvider, eventPublisher: eventPublisher);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.BackupFilePath);
        Assert.True(File.Exists(result.BackupFilePath));
        Assert.Equal(1, indexProvider.CloseAllCallCount);

        using var archive = ZipFile.OpenRead(result.BackupFilePath!);
        var entryNames = archive.Entries.Select(e => e.FullName).ToHashSet();
        Assert.Equal(
            new HashSet<string> {
                "backup-manifest.json", "app.config", "database/app.db",
                "search-index/segment1.bin", "search-index/segment2.bin"
            },
            entryNames);

        var manifestEntry = archive.GetEntry("backup-manifest.json")!;
        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestStream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal(1, manifest.FormatVersion);
        Assert.Equal("InfoPhoenix", manifest.Application);
        Assert.Equal("v1.4.0", manifest.AppVersion);
        Assert.Equal("20261201093000_Initial", manifest.DatabaseMigrationId);
        Assert.True(manifest.Contents.Database);
        Assert.True(manifest.Contents.SearchIndex);
        Assert.True(manifest.Contents.Configuration);

        var successEvent = Assert.Single(
            eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>(),
            e => e.Type == NotificationType.Success);
        Assert.Contains(result.BackupFilePath!, successEvent.Message);

        // The archived DB is a readable, independent copy (plan D7/quality attribute "data integrity").
        var extractDirectory = Directory.CreateTempSubdirectory("ipx-0009-extract-").FullName;
        try {
            archive.GetEntry("database/app.db")!.ExtractToFile(Path.Combine(extractDirectory, "app.db"));

            using var connection = new SqliteConnection($"Data Source={Path.Combine(extractDirectory, "app.db")};Mode=ReadOnly;Pooling=false;");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;";
            Assert.Equal("20261201093000_Initial", command.ExecuteScalar());
        }
        finally {
            Directory.Delete(extractDirectory, recursive: true);
        }
    }

    /// <summary>
    /// B1 complement: a source database left in WAL mode (as EF Core's live connection leaves
    /// it, with its own <c>-wal</c>/<c>-shm</c> sidecar files sitting next to <c>app.db</c>)
    /// never leaks those sidecar files into the archive - the staged copy is forced out of WAL
    /// mode before it is zipped (review finding #1). The seeding connection is kept open for the
    /// whole test, as a live app connection would be, so the sidecar files actually exist on
    /// disk when the backup runs - closing it first would let SQLite checkpoint them away on its
    /// own, defeating the point of the test.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithTheSourceDatabaseInWalMode_ExcludesWalAndShmFromTheArchive() {
        using var liveConnection = SeedDatabaseKeptOpenInWalMode();

        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        using var archive = ZipFile.OpenRead(result.BackupFilePath!);
        Assert.DoesNotContain(archive.Entries, e => e.FullName == "database/app.db-wal");
        Assert.DoesNotContain(archive.Entries, e => e.FullName == "database/app.db-shm");
        Assert.Contains(archive.Entries, e => e.FullName == "database/app.db");
    }

    /// <summary>B1 complement: a <c>write.lock</c> present in the index never reaches the archive.</summary>
    [Fact]
    public async Task HandleAsync_WithWriteLockPresentInIndex_ExcludesItFromTheArchive() {
        SeedIndex("segment1.bin");
        File.WriteAllText(Path.Combine(_luceneRoot, LuceneRelativePath, "write.lock"), string.Empty);

        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        using var archive = ZipFile.OpenRead(result.BackupFilePath!);
        Assert.DoesNotContain(archive.Entries, e => e.FullName.EndsWith("write.lock", StringComparison.Ordinal));
        Assert.Contains(archive.Entries, e => e.FullName == "search-index/segment1.bin");
    }

    /// <summary>
    /// B12: a directory junction under the index root is a reparse point - production code
    /// already checks <see cref="FileAttributes.ReparsePoint"/> and skips it (plan §5); this
    /// proves it, rather than requiring elevation (a directory junction, unlike a symbolic link,
    /// needs none).
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithAReparsePointJunctionUnderTheIndexRoot_ExcludesTheLinkedContentFromTheArchive() {
        SeedIndex("segment1.bin");
        var indexDirectory = Path.Combine(_luceneRoot, LuceneRelativePath);
        var targetDirectory = Directory.CreateTempSubdirectory("ipx-0009-junction-target-").FullName;
        File.WriteAllText(Path.Combine(targetDirectory, "secret.bin"), "outside-content");
        var junctionPath = Path.Combine(indexDirectory, "linked");

        CreateDirectoryJunction(junctionPath, targetDirectory);

        try {
            var applicationContext = new FakeApplicationContext {
                FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
            };
            var handler = CreateHandler(applicationContext);

            var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

            Assert.True(result.Succeeded);
            using var archive = ZipFile.OpenRead(result.BackupFilePath!);
            Assert.DoesNotContain(archive.Entries, e => e.FullName.Contains("secret.bin", StringComparison.Ordinal));
            Assert.Contains(archive.Entries, e => e.FullName == "search-index/segment1.bin");
        }
        finally {
            Directory.Delete(junctionPath);
            Directory.Delete(targetDirectory, recursive: true);
        }
    }

    /// <summary>B2: cleanup also runs on the success path, not only on failure.</summary>
    [Fact]
    public async Task HandleAsync_AfterASuccessfulRun_LeavesNoStagingFolderBehind() {
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "backup-*").Length == 0,
            "Expected no leftover 'backup-*' staging folder after a successful run.");
    }

    /// <summary>
    /// B3/D6: no artifact present still succeeds, with exactly 3 warnings and nothing archived
    /// except the manifest. The live app.db must not be created as a side effect.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithNoArtifactsPresent_SucceedsWithThreeWarnings_AndDoesNotCreateTheLiveDatabase() {
        var eventPublisher = new FakeEventPublisher();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, eventPublisher: eventPublisher);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);

        using var archive = ZipFile.OpenRead(result.BackupFilePath!);
        Assert.Equal(["backup-manifest.json"], archive.Entries.Select(e => e.FullName));

        var manifestEntry = archive.GetEntry("backup-manifest.json")!;
        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestStream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.False(manifest.Contents.Database);
        Assert.False(manifest.Contents.SearchIndex);
        Assert.False(manifest.Contents.Configuration);

        var warnings = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>()
            .Where(e => e.Type == NotificationType.Warning)
            .ToList();
        Assert.Equal(3, warnings.Count);

        Assert.False(File.Exists(Path.Combine(_appRoot, "databases", "app.db")));
    }

    /// <summary>B4: a DB file with no migrations history table still succeeds, with a null migration id.</summary>
    [Fact]
    public async Task HandleAsync_WithDatabaseButNoMigrationsHistoryTable_SucceedsWithNullMigrationId() {
        SeedDatabase(withMigrationHistory: false);

        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);

        using var archive = ZipFile.OpenRead(result.BackupFilePath!);
        var manifestEntry = archive.GetEntry("backup-manifest.json")!;
        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestStream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.True(manifest.Contents.Database);
        Assert.Null(manifest.DatabaseMigrationId);
    }

    /// <summary>B5: an unwritable backup target (a file, not a directory) fails cleanly.</summary>
    [Fact]
    public async Task HandleAsync_WhenTheBackupTargetIsAFileNotADirectory_FailsCleanly() {
        File.WriteAllText(Path.Combine(_appRoot, "backups"), string.Empty);

        var eventPublisher = new FakeEventPublisher();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, eventPublisher: eventPublisher);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.BackupFilePath);

        var failures = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>()
            .Where(e => e.Type == NotificationType.Failure)
            .ToList();
        var failure = Assert.Single(failures);
        Assert.StartsWith("PerformApplicationBackupRequestHandler_CreatingArchive_Failure", failure.Message);

        Assert.False(Directory.Exists(Path.Combine(_appRoot, "temporary")) &&
            Directory.GetDirectories(Path.Combine(_appRoot, "temporary"), "backup-*").Length > 0);
    }

    /// <summary>B6: a locked index file fails cleanly, with staging cleaned up.</summary>
    [Fact]
    public async Task HandleAsync_WhenAnIndexFileIsLocked_FailsCleanly_WithStagingCleanedUp() {
        SeedIndex("segment1.bin");
        var lockedFilePath = Path.Combine(_luceneRoot, LuceneRelativePath, "segment1.bin");

        var eventPublisher = new FakeEventPublisher();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, eventPublisher: eventPublisher);

        await using var lockedFile = new FileStream(lockedFilePath, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.False(result.Succeeded);

        var failures = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>()
            .Where(e => e.Type == NotificationType.Failure)
            .ToList();
        var failure = Assert.Single(failures);
        Assert.StartsWith("PerformApplicationBackupRequestHandler_CopyingSearchIndex_Failure", failure.Message);

        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "backup-*").Length == 0);
    }

    /// <summary>B7: <c>CloseAll</c> throwing an <see cref="AggregateException"/> fails cleanly.</summary>
    [Fact]
    public async Task HandleAsync_WhenCloseAllThrows_FailsCleanly_WithExactlyOneFailureAndZeroSuccess() {
        var indexProvider = new FakeIndexProvider {
            ThrowOnCloseAll = new AggregateException(new InvalidOperationException("index busy"))
        };
        var eventPublisher = new FakeEventPublisher();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, indexProvider: indexProvider, eventPublisher: eventPublisher);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.False(result.Succeeded);

        var progressEvents = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>().ToList();
        var failure = Assert.Single(progressEvents, e => e.Type == NotificationType.Failure);
        Assert.StartsWith("PerformApplicationBackupRequestHandler_CopyingSearchIndex_Failure", failure.Message);
        Assert.DoesNotContain(progressEvents, e => e.Type == NotificationType.Success);

        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "backup-*").Length == 0);
    }

    /// <summary>
    /// B8: a cancel requested right before the manifest write, the zip and the move still
    /// propagates <see cref="OperationCanceledException"/>, publishes no Failure entry, and
    /// leaves nothing behind. The token is cancelled from inside the "CreatingArchive" step's own
    /// Information publish - the earliest point reachable with a publish-based hook. This is an
    /// earlier cancellation point than the explicit commit-point check itself (see the
    /// complementary test below, review finding #7): <c>WriteManifestAsync</c>'s own
    /// cancellation check throws before that explicit check is ever reached.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenCancelledRightBeforeTheCommitPoint_PropagatesCancellation_WithNothingLeftBehind() {
        using var cts = new CancellationTokenSource();
        var eventPublisher = new FakeEventPublisher {
            OnPublish = @event => {
                // Plan IPX-0015 note: the real SectionTranslator's single-key indexer always
                // forwards through the params-array overload (even with zero args), so this
                // fake's info-step messages carry a trailing ": " - StartsWith, not an exact
                // match, is what survives the real composition.
                if (@event is BackupRecoveryProgressNotificationEvent evt &&
                    evt.Message.StartsWith("PerformApplicationBackupRequestHandler_CreatingArchive", StringComparison.Ordinal)) {
                    cts.Cancel();
                }
            }
        };
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, eventPublisher: eventPublisher);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(new PerformApplicationBackupRequest(), cts.Token));

        Assert.DoesNotContain(
            eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>(),
            e => e.Type is NotificationType.Failure or NotificationType.Success);

        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "backup-*").Length == 0);

        var backupDirectory = Path.Combine(_appRoot, "backups");
        Assert.True(!Directory.Exists(backupDirectory) || Directory.GetFiles(backupDirectory).Length == 0);
    }

    /// <summary>
    /// B8 complement (review finding #7): a cancel requested strictly after the manifest and the
    /// zip are already written - right before the move - still propagates
    /// <see cref="OperationCanceledException"/> through the explicit commit-point check (plan
    /// §3.2), not through an earlier await's own check. No publish event exists in that narrow
    /// window to hook a test off, so this uses the handler's internal, test-only
    /// <c>OnBeforeCommitPointCheck</c> hook instead of racing real timing.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenCancelledAfterTheArchiveIsWritten_PropagatesCancellation_ThroughTheCommitPointCheck() {
        using var cts = new CancellationTokenSource();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext);
        handler.OnBeforeCommitPointCheck = cts.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(new PerformApplicationBackupRequest(), cts.Token));

        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "backup-*").Length == 0);

        var backupDirectory = Path.Combine(_appRoot, "backups");
        Assert.True(!Directory.Exists(backupDirectory) || Directory.GetFiles(backupDirectory).Length == 0);
    }

    /// <summary>
    /// B9/AC3: two runs with the same frozen instant produce two distinct files - the second
    /// gets the "-2" suffix - and the first file's bytes are unchanged by the second run.
    /// </summary>
    [Fact]
    public async Task HandleAsync_CalledTwiceWithTheSameFrozenInstant_ProducesTwoDistinctFiles() {
        var timeProvider = new FakeTimeProvider();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, timeProvider: timeProvider);

        var first = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);
        var firstBytes = await File.ReadAllBytesAsync(first.BackupFilePath!);

        var second = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.NotEqual(first.BackupFilePath, second.BackupFilePath);
        Assert.EndsWith("Z-2.zip", second.BackupFilePath);
        Assert.True(File.Exists(first.BackupFilePath));
        Assert.True(File.Exists(second.BackupFilePath));
        Assert.Equal(firstBytes, await File.ReadAllBytesAsync(first.BackupFilePath!));
    }

    /// <summary>B10 (should-have): a stale orphan staging folder is swept on the next run.</summary>
    [Fact]
    public async Task HandleAsync_WithAStaleOrphanStagingFolder_SweepsItOnTheNextRun() {
        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        var orphanDirectory = Path.Combine(temporaryDirectory, "backup-orphan");
        Directory.CreateDirectory(orphanDirectory);
        File.WriteAllText(Path.Combine(orphanDirectory, "leftover.txt"), "stale");

        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(Directory.Exists(orphanDirectory));
    }

    /// <summary>B11 (should-have): collision exhaustion (99 names taken) fails cleanly.</summary>
    [Fact]
    public async Task HandleAsync_WhenAllCollisionSuffixesAreTaken_FailsCleanly() {
        var timeProvider = new FakeTimeProvider();
        var backupsDirectory = Path.Combine(_appRoot, "backups");
        Directory.CreateDirectory(backupsDirectory);

        var baseName = string.Format(
            CultureInfo.InvariantCulture, Constants.BackupRecovery.FileNamePattern, timeProvider.UtcNow.UtcDateTime);
        File.WriteAllText(Path.Combine(backupsDirectory, baseName), "taken");
        for (var attempt = 2; attempt <= Constants.BackupRecovery.MaxCollisionSuffix; attempt++) {
            var name = $"{Path.GetFileNameWithoutExtension(baseName)}-{attempt}{Path.GetExtension(baseName)}";
            File.WriteAllText(Path.Combine(backupsDirectory, name), "taken");
        }

        var eventPublisher = new FakeEventPublisher();
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };
        var handler = CreateHandler(applicationContext, timeProvider: timeProvider, eventPublisher: eventPublisher);

        var result = await handler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);

        Assert.False(result.Succeeded);
        var failure = Assert.Single(
            eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>(),
            e => e.Type == NotificationType.Failure);
        Assert.StartsWith("PerformApplicationBackupRequestHandler_CreatingArchive_Failure", failure.Message);

        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "backup-*").Length == 0);
    }

    private PerformApplicationBackupRequestHandler CreateHandler(
        FakeApplicationContext applicationContext,
        FakeIndexProvider? indexProvider = null,
        FakeTimeProvider? timeProvider = null,
        FakeEventPublisher? eventPublisher = null) {
        return new PerformApplicationBackupRequestHandler(
            applicationContext,
            new FakeAppConfigurationManager(),
            indexProvider ?? new FakeIndexProvider(),
            new PhysicalFileManagerFactory().GetOrCreate(_luceneRoot),
            Options.Create(new LuceneOptions { DirectoryRelativePath = LuceneRelativePath }),
            timeProvider ?? new FakeTimeProvider(),
            eventPublisher ?? new FakeEventPublisher(),
            new FormattingTranslator().For<PerformApplicationBackupRequestHandler>(),
            NullLogger<PerformApplicationBackupRequestHandler>.Instance
        );
    }

    private void SeedDatabase(bool withMigrationHistory) {
        var databaseDirectory = Path.Combine(_appRoot, "databases");
        Directory.CreateDirectory(databaseDirectory);
        var databasePath = Path.Combine(databaseDirectory, "app.db");

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false;");
        connection.Open();

        using var command = connection.CreateCommand();
        if (withMigrationHistory) {
            command.CommandText = """
                CREATE TABLE __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT);
                INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261201093000_Initial', '10.0.0');
                """;
        }
        else {
            command.CommandText = "CREATE TABLE SomeOtherTable (Id INTEGER PRIMARY KEY);";
        }

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Seeds <c>app.db</c> in WAL mode and returns the still-open connection, so its
    /// <c>-wal</c>/<c>-shm</c> sidecar files genuinely exist on disk for the caller's test -
    /// closing this connection lets SQLite checkpoint them away.
    /// </summary>
    private SqliteConnection SeedDatabaseKeptOpenInWalMode() {
        var databaseDirectory = Path.Combine(_appRoot, "databases");
        Directory.CreateDirectory(databaseDirectory);
        var databasePath = Path.Combine(databaseDirectory, "app.db");

        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false;");
        connection.Open();

        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA journal_mode='wal';";
        pragmaCommand.ExecuteNonQuery();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT);
            INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261201093000_Initial', '10.0.0');
            """;
        command.ExecuteNonQuery();

        return connection;
    }

    private void SeedIndex(params string[] fileNames) {
        var indexDirectory = Path.Combine(_luceneRoot, LuceneRelativePath);
        Directory.CreateDirectory(indexDirectory);

        foreach (var fileName in fileNames) {
            File.WriteAllText(Path.Combine(indexDirectory, fileName), "lucene-data");
        }
    }

    /// <summary>
    /// Creates a directory junction at <paramref name="linkPath"/> pointing at
    /// <paramref name="targetPath"/> via <c>mklink /J</c> - unlike a symbolic link
    /// (<c>mklink /D</c>), a junction needs no elevation, so this runs unattended in CI.
    /// </summary>
    private static void CreateDirectoryJunction(string linkPath, string targetPath) {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", $"""/c mklink /J "{linkPath}" "{targetPath}" """) {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);
    }

    /// <summary>
    /// Unlike the shared <c>FakeTranslator</c> (which echoes the key and discards format
    /// arguments), this formats them in - needed to assert that the Success/Failure message
    /// actually names the file path / exception message (plan AC1/§3.2), not just the key.
    /// </summary>
    private sealed class FormattingTranslator : ITranslator {
        public event PropertyChangedEventHandler? PropertyChanged;

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

        public string this[string key] => key;

        public string this[string key, object[] args] => $"{key}: {string.Join(", ", args)}";

        public void SetCulture(CultureInfo culture) {
            CurrentCulture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        public void SetCulture(string isoCode) {
            SetCulture(CultureInfo.GetCultureInfo(isoCode));
        }
    }
}

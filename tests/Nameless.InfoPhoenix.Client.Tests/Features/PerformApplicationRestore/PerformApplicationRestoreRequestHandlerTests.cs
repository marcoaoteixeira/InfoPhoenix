using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.IO.Physical;
using Nameless.Lucene;
using Nameless.Windows.Events;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.BackupRecovery;

namespace Nameless.InfoPhoenix.Client.Tests.Features.PerformApplicationRestore;

/// <summary>
/// Exercises <see cref="PerformApplicationRestoreRequestHandler"/>. Same two-root shape as the
/// IPX-0009 backup tests: <see cref="_appRoot"/> stands in for
/// <see cref="Nameless.Application.IApplicationContext.FileManager"/> (config/DB/backups/
/// temporary), <see cref="_luceneRoot"/> for the DI-registered <c>IFileManager</c> that resolves
/// the Lucene index (the R1 split).
/// </summary>
public sealed class PerformApplicationRestoreRequestHandlerTests : IDisposable {
    private readonly string _appRoot = Directory.CreateTempSubdirectory("ipx-0011-app-").FullName;
    private readonly string _luceneRoot = Directory.CreateTempSubdirectory("ipx-0011-lucene-").FullName;

    private const string LuceneRelativePath = "databases/lucene";

    public void Dispose() {
        Directory.Delete(_appRoot, recursive: true);
        Directory.Delete(_luceneRoot, recursive: true);
    }

    private string LiveConfigPath => Path.Combine(_appRoot, "app.config");
    private string LiveDatabasePath => Path.Combine(_appRoot, "databases", "app.db");
    private string LiveIndexDirectory => Path.Combine(_luceneRoot, "databases", "lucene");

    // ----- AC1: compatibility theory (B1) ------------------------------------------------------

    [Theory]
    [InlineData(2, "InfoPhoenix", null, "Rejected_UnsupportedFormat")]
    [InlineData(1, "Other", null, "Rejected_WrongApplication")]
    public async Task HandleAsync_WithAnIncompatibleManifest_IsBlocked_AndTouchesNothing(
        int formatVersion, string application, string? databaseMigrationId, string expectedFailureKeyPrefix) {
        var zipPath = BuildZip(z => AddManifest(z, formatVersion: formatVersion, application: application, databaseMigrationId: databaseMigrationId));
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, expectedFailureKeyPrefix);
        AssertSnapshotUnchanged(before);
        Assert.False(Directory.Exists(Path.Combine(_appRoot, "temporary")));
    }

    [Fact]
    public async Task HandleAsync_WithASchemaMismatch_AndDatabasePresent_IsBlocked() {
        SeedLiveDatabase(withMigrationHistory: false); // today's app has no compiled migrations (expected = null)
        var zipPath = BuildZip(z => {
            AddManifest(z, databaseMigrationId: "20990101000000_Future", database: true);
            AddDatabaseEntry(z, withMigrationHistory: true, migrationId: "20990101000000_Future");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_SchemaMismatch");
    }

    [Fact]
    public async Task HandleAsync_WithAManifestMissingContents_IsBlocked() {
        var zipPath = BuildZip(z => {
            var entry = z.CreateEntry("backup-manifest.json");
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write("""{"formatVersion":1,"application":"InfoPhoenix","appVersion":"v1.0.0"}"""); // no contents/createdAtUtc
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ManifestInvalid");
    }

    // ----- AC1/D1.4: version-differs warning theory (B23) ---------------------------------------

    [Fact]
    public async Task HandleAsync_WithADifferentAppVersion_AndMatchingSchema_StillCompletes_WithExactlyOneWarning() {
        SeedLiveConfig("{}");
        var zipPath = BuildZip(z => {
            AddManifest(z, appVersion: "v0.9.0", configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher); // running version is v1.0.0 (CreateHandler)

        Assert.Equal(RestoreOutcome.Completed, result);
        var warnings = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>()
            .Where(e => e.Type == NotificationType.Warning).ToList();
        var warning = Assert.Single(warnings);
        Assert.StartsWith("PerformApplicationRestoreRequestHandler_VersionDiffers_Warning", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WithAnUnparseableAppVersion_StillCompletes_WithExactlyOneWarning() {
        SeedLiveConfig("{}");
        var zipPath = BuildZip(z => {
            AddManifest(z, appVersion: "not-a-semver", configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Completed, result);
        var warnings = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>()
            .Where(e => e.Type == NotificationType.Warning).ToList();
        Assert.Single(warnings);
    }

    [Fact]
    public async Task HandleAsync_WithTheSameVersionDifferingOnlyInBuildMetadata_Completes_WithNoWarning() {
        SeedLiveConfig("{}");
        var zipPath = BuildZip(z => {
            AddManifest(z, appVersion: "v1.0.0+differentbuild", configuration: true); // running version is v1.0.0 (CreateHandler)
            WriteRawEntry(z, "app.config", "{}");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.DoesNotContain(
            eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>(),
            e => e.Type == NotificationType.Warning);
    }

    // ----- AC1: untrusted-zip entry theory (B2) -------------------------------------------------

    [Theory]
    [InlineData("../x")]
    [InlineData("search-index/../../x")]
    [InlineData("/abs")]
    [InlineData("C:x")]
    [InlineData("app.config:ads")]
    [InlineData("search-index/CON")]
    [InlineData("search-index/con.txt")]
    [InlineData("search-index/COM0")]
    [InlineData("search-index/LPT0")]
    [InlineData("search-index/CONIN$")]
    [InlineData("search-index/CONOUT$")]
    [InlineData("search-index/COM¹")]
    [InlineData("search-index/LPT²")]
    [InlineData("app.config.")]
    [InlineData("app.config ")]
    [InlineData("search-index/sub/./x")]
    [InlineData("search-index/sub/../x")]
    [InlineData("search-index/x\u0000y")]
    [InlineData("other.txt")]
    public async Task HandleAsync_WithAnUnsafeOrUnexpectedEntryName_IsBlocked_AndTouchesNothing(string unsafeName) {
        var zipPath = BuildZip(z => {
            AddManifest(z);
            WriteRawEntry(z, unsafeName, "x");
        });
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_UnexpectedEntry");
        AssertSnapshotUnchanged(before);
        Assert.False(Directory.Exists(Path.Combine(_appRoot, "temporary")));
    }

    [Fact]
    public async Task HandleAsync_WithANameOver260Characters_IsBlocked() {
        var longName = "search-index/" + new string('a', 300);
        var zipPath = BuildZip(z => {
            AddManifest(z);
            WriteRawEntry(z, longName, "x");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_UnexpectedEntry");
    }

    [Fact]
    public async Task HandleAsync_WithADuplicateCaseInsensitiveEntryName_IsBlocked() {
        var zipPath = BuildZip(z => {
            AddManifest(z);
            WriteRawEntry(z, "app.config", "a");
            WriteRawEntry(z, "APP.CONFIG", "b");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_UnexpectedEntry");
    }

    [Fact]
    public async Task HandleAsync_WithMoreThanTheMaximumEntryCount_IsBlocked_WithACappedMessage() {
        var zipPath = BuildZip(z => {
            AddManifest(z);
            for (var i = 0; i < Constants.BackupRecovery.MaxArchiveEntryCount + 1; i++) {
                WriteRawEntry(z, $"search-index/f{i}.bin", "x");
            }
        });
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        var failure = AssertSingleFailureStartsWith(eventPublisher, "Rejected_TooManyEntries");
        Assert.Contains(Constants.BackupRecovery.MaxArchiveEntryCount.ToString(), failure);
        AssertSnapshotUnchanged(before);
    }

    /// <summary>
    /// B4: a central-directory "uncompressed size" declaring far less than the entry's real
    /// content, on a Stored (uncompressed) entry. <c>System.IO.Compression</c>'s own read
    /// stream does not truncate a Stored entry's reads to the declared length (verified
    /// directly against the BCL), so the patched header's bytes flow straight through to
    /// <c>ExtractEntryBounded</c> - which now catches it via the short-read/length check added
    /// for this ticket (the loop finishes having written more than the lying declared length,
    /// well before the CRC-32 check would even run).
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithALyingSizeEntry_IsBlocked() {
        var zipPath = BuildZipWithLyingSizeEntry();
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_Corrupted");
        AssertSnapshotUnchanged(before);
        AssertNoStagingLeftover();
    }

    /// <summary>
    /// A Stored entry whose declared size and local-file-header CRC-32 both still match its
    /// original content, but whose actual bytes were tampered with after the fact - the one
    /// scenario the length checks alone cannot catch, and the reason the CRC-32 check was added
    /// for this ticket (plan D3's "CRC-failing entries throw").
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithATamperedButCorrectlySizedEntry_IsBlocked() {
        var zipPath = BuildZipWithTamperedEntry();
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_Corrupted");
        AssertSnapshotUnchanged(before);
        AssertNoStagingLeftover();
    }

    [Fact]
    public async Task HandleAsync_WithContentsFlagsDisagreeingWithEntries_IsBlocked() {
        var zipPath = BuildZip(z => AddManifest(z, database: true)); // claims a DB, but no database/app.db entry
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ContentsMismatch");
    }

    // ----- AC1: manifest problems (B6) ----------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithAFileThatIsNotAZip_IsBlocked() {
        var path = Path.Combine(Directory.CreateTempSubdirectory("ipx-0011-src-").FullName, "not-a-zip.zip");
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]);
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(path, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_NotAnArchive");
    }

    [Fact]
    public async Task HandleAsync_WithAZipWithNoManifest_IsBlocked() {
        var zipPath = BuildZip(z => WriteRawEntry(z, "app.config", "{}"));
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ManifestMissing");
    }

    [Fact]
    public async Task HandleAsync_WithAManifestOverTheSizeLimit_IsBlocked() {
        var zipPath = BuildZip(z => WriteRawEntry(z, "backup-manifest.json", new string(' ', Constants.BackupRecovery.MaxManifestBytes + 1)));
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ManifestInvalid");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJsonManifest_IsBlocked() {
        var zipPath = BuildZip(z => WriteRawEntry(z, "backup-manifest.json", "{ not json"));
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ManifestInvalid");
    }

    // ----- AC1: DB verification (B8) -------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithAGarbageDatabaseFile_IsBlocked() {
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, databaseMigrationId: null);
            WriteRawEntry(z, "database/app.db", "this is not a sqlite file, just garbage bytes padded out a bit");
        });
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_DatabaseInvalid");
        AssertSnapshotUnchanged(before);
    }

    [Fact]
    public async Task HandleAsync_WhenMigrationsHistoryDisagreesWithTheManifest_IsBlocked() {
        // The manifest's claim (null) must match the app's expected migration id (also null,
        // today) to pass the D1 compatibility check - only then does step 3 reach the DB itself
        // and find it disagrees with what the manifest claimed.
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, databaseMigrationId: null);
            AddDatabaseEntry(z, withMigrationHistory: true, migrationId: "actual-id-in-the-db");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ContentsMismatch");
    }

    [Fact]
    public async Task HandleAsync_WhenMigrationsHistoryIsAViewNotATable_IsBlocked() {
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, databaseMigrationId: null);
            AddDatabaseEntryWithMigrationsHistoryAsView(z);
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_DatabaseInvalid");
    }

    // ----- AC1: configuration validation (B9) -----------------------------------------------------

    [Fact]
    public async Task HandleAsync_WithAConfigurationThatIsAJsonArray_IsBlocked() {
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", "[1,2,3]");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_ConfigurationInvalid");
    }

    // ----- D2: selected-file guard (B10) ----------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenTheSelectedFileIsUnderTheTemporaryFolder_IsBlocked_WithoutReadingIt() {
        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Directory.CreateDirectory(temporaryDirectory);
        var path = Path.Combine(temporaryDirectory, "picked.zip");
        File.WriteAllBytes(path, [1, 2, 3]); // not even a valid zip - proves it is never opened
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(path, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_SelectedFileInDataFolder");
    }

    // ----- AC2: happy path and mirror semantics (B11-B13) -------------------------------------------

    [Fact]
    public async Task HandleAsync_WithAllThreeArtifactsPresentAndDifferent_Completes_AndSwapsEverythingAtomically() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        SeedLiveDatabase(withMigrationHistory: false, markerValue: "old-db");
        SeedLiveIndex(("segment1.bin", "old-index-data"));

        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, searchIndex: true, configuration: true, databaseMigrationId: null);
            WriteRawEntry(z, "app.config", """{"Theme":"Dark"}""");
            AddDatabaseEntry(z, withMigrationHistory: false, migrationId: null, markerValue: "new-db");
            WriteRawEntry(z, "search-index/segment1.bin", "new-index-data");
        });

        var indexProvider = new FakeIndexProvider();
        var appConfigurationManager = new FakeAppConfigurationManager();
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(
            zipPath, indexProvider: indexProvider, appConfigurationManager: appConfigurationManager, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.Equal("""{"Theme":"Dark"}""", File.ReadAllText(LiveConfigPath));
        Assert.Equal("new-index-data", File.ReadAllText(Path.Combine(LiveIndexDirectory, "segment1.bin")));
        Assert.Equal(1, indexProvider.CloseAllCallCount);
        Assert.Contains("Reload", appConfigurationManager.CallLog);

        using (var connection = new SqliteConnection($"Data Source={LiveDatabasePath};Mode=ReadOnly;Pooling=false;")) {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Marker;";
            Assert.Equal("new-db", command.ExecuteScalar());
        }

        Assert.Empty(Directory.GetFiles(Path.Combine(_appRoot, "databases"), "*.restore-old-*"));
        Assert.Empty(Directory.GetDirectories(_luceneRoot, "*.restore-old-*", SearchOption.AllDirectories));
        AssertNoStagingLeftover();

        var progress = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>().ToList();
        Assert.Equal(NotificationType.Success, progress.Last().Type);
        Assert.Single(progress, e => e.Type == NotificationType.Success);
        var informationSteps = progress.Where(e => e.Type == NotificationType.Information).Select(e => e.Message).ToList();
        Assert.Equal(
            ["PerformApplicationRestoreRequestHandler_ReadingBackup",
             "PerformApplicationRestoreRequestHandler_ExtractingBackup",
             "PerformApplicationRestoreRequestHandler_VerifyingBackup",
             "PerformApplicationRestoreRequestHandler_PreparingSwap",
             "PerformApplicationRestoreRequestHandler_Swapping"],
            informationSteps);
    }

    [Fact]
    public async Task HandleAsync_WithAStaleWalSidecarBeforeRestore_LeavesNoSidecarNextToTheRestoredDatabase() {
        SeedLiveDatabase(withMigrationHistory: false, markerValue: "old-db");
        File.WriteAllText(LiveDatabasePath + "-wal", "stale-wal-bytes");
        File.WriteAllText(LiveDatabasePath + "-shm", "stale-shm-bytes");

        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, databaseMigrationId: null);
            AddDatabaseEntry(z, withMigrationHistory: false, migrationId: null, markerValue: "new-db");
        });

        var result = await RunAsync(zipPath);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.False(File.Exists(LiveDatabasePath + "-wal"));
        Assert.False(File.Exists(LiveDatabasePath + "-shm"));

        using var connection = new SqliteConnection($"Data Source={LiveDatabasePath};Mode=ReadOnly;Pooling=false;");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Marker;";
        Assert.Equal("new-db", command.ExecuteScalar());
    }

    [Fact]
    public async Task HandleAsync_WhenTheBackupHasConfigOnly_RemovesTheLiveDatabaseAndIndex_WithWarnings() {
        SeedLiveDatabase(withMigrationHistory: false);
        SeedLiveIndex(("segment1.bin", "data"));
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        var eventPublisher = new FakeEventPublisher();

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.False(File.Exists(LiveDatabasePath));
        Assert.False(Directory.Exists(LiveIndexDirectory));
        Assert.True(File.Exists(LiveConfigPath));

        var warnings = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>()
            .Where(e => e.Type == NotificationType.Warning).Select(e => e.Message).ToList();
        Assert.Contains(warnings, m => m.StartsWith("PerformApplicationRestoreRequestHandler_DatabaseAbsent_Warning", StringComparison.Ordinal));
        Assert.Contains(warnings, m => m.StartsWith("PerformApplicationRestoreRequestHandler_SearchIndexAbsent_Warning", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_OnAFreshInstallWithNoLiveArtifacts_Completes_AndCreatesEverything() {
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, searchIndex: true, configuration: true, databaseMigrationId: null);
            WriteRawEntry(z, "app.config", "{}");
            AddDatabaseEntry(z, withMigrationHistory: false, migrationId: null);
            WriteRawEntry(z, "search-index/segment1.bin", "data");
        });

        var result = await RunAsync(zipPath);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.True(File.Exists(LiveConfigPath));
        Assert.True(File.Exists(LiveDatabasePath));
        Assert.True(File.Exists(Path.Combine(LiveIndexDirectory, "segment1.bin")));
    }

    // ----- AC3: pre-commit failures (extraction/free space) -----------------------------------------

    [Fact]
    public async Task HandleAsync_WhenTheFreeSpacePreflightFails_IsBlocked_AndLeavesStagingDeleted() {
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, databaseMigrationId: null);
            WriteRawEntry(z, "database/app.db", "small");
        });
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();
        var handler = CreateHandler(eventPublisher: eventPublisher);
        handler.AvailableFreeSpaceOverride = _ => 1; // Stubbed seam (plan B5): pretend the disk is full.

        var result = await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), CancellationToken.None);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_InsufficientSpace");
        AssertSnapshotUnchanged(before);
        AssertNoStagingLeftover();
    }

    [Fact]
    public async Task HandleAsync_WhenTheInstallVolumeFreeSpacePreflightFails_IsBlocked() {
        var zipPath = BuildZip(z => {
            AddManifest(z, searchIndex: true);
            WriteRawEntry(z, "search-index/segment1.bin", "data");
        });
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();
        var handler = CreateHandler(eventPublisher: eventPublisher);
        handler.ForceCrossVolumeIndexNeeded = true;
        // The staging-volume check (keyed by the staging path) is left generous; only the
        // install-volume check (keyed by the live index's root) must be the one that fails.
        handler.AvailableFreeSpaceOverride = path =>
            string.Equals(path, Path.GetPathRoot(LiveIndexDirectory), StringComparison.OrdinalIgnoreCase) ? 1 : long.MaxValue;

        var result = await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), CancellationToken.None);

        Assert.Equal(RestoreOutcome.Blocked, result);
        AssertSingleFailureStartsWith(eventPublisher, "Rejected_InsufficientSpace");
        AssertSnapshotUnchanged(before);
        AssertNoStagingLeftover();
    }

    /// <summary>
    /// Review finding: before the fix, the cross-volume sibling path was only captured for
    /// cleanup *after* <c>PrepareSwap</c> returned - a copy failure partway through PrepareSwap
    /// meant the sibling folder's path was never recorded, leaking it under the live index's
    /// parent folder until the next run's sweep.
    /// <see cref="PerformApplicationRestoreRequestHandler.ForceCrossVolumeIndexNeeded"/> and
    /// <see cref="PerformApplicationRestoreRequestHandler.OnBeforeCrossVolumeCopy"/> make this
    /// deterministic without a real second drive.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenTheCrossVolumeIndexCopyFailsPartway_LeavesNoLeakedSiblingFolder() {
        var zipPath = BuildZip(z => {
            AddManifest(z, searchIndex: true);
            WriteRawEntry(z, "search-index/segment1.bin", "data");
        });
        var eventPublisher = new FakeEventPublisher();
        var handler = CreateHandler(eventPublisher: eventPublisher);
        handler.ForceCrossVolumeIndexNeeded = true;
        handler.OnBeforeCrossVolumeCopy = () => throw new IOException("simulated disk full on the install volume");

        var result = await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), CancellationToken.None);

        Assert.Equal(RestoreOutcome.RolledBack, result);
        AssertSingleFailureStartsWith(eventPublisher, "PreparingSwap_Failure");
        Assert.Empty(Directory.GetDirectories(
            _luceneRoot, $"*{Constants.BackupRecovery.RestoreNewSuffix}*", SearchOption.AllDirectories));
    }

    // ----- AC3: in-commit failure, rollback and cancellation ------------------------------------------

    /// <summary>
    /// B14: holds the LIVE app.config open for real (FileShare.Read, no delete share) for the
    /// whole commit, so move 6 (configuration aside) fails for real, strictly after the DB move
    /// already succeeded - proving a real OS rename reversal, not a mocked throw.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenTheLiveConfigFileIsHeldOpenDuringCommit_RollsBackForReal() {
        SeedLiveDatabase(withMigrationHistory: false);
        SeedLiveConfig("""{"Theme":"Light"}""");
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, configuration: true, databaseMigrationId: null);
            AddDatabaseEntry(z, withMigrationHistory: false, migrationId: null);
            WriteRawEntry(z, "app.config", """{"Theme":"Dark"}""");
        });

        var dbBefore = File.ReadAllBytes(LiveDatabasePath);
        var configBefore = File.ReadAllBytes(LiveConfigPath);
        var appConfigurationManager = new FakeAppConfigurationManager();
        var eventPublisher = new FakeEventPublisher();

        await using var heldOpen = new FileStream(LiveConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var result = await RunAsync(zipPath, appConfigurationManager: appConfigurationManager, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.RolledBack, result);
        AssertSingleFailureStartsWith(eventPublisher, "Swapping_Failure");
        Assert.Equal(dbBefore, File.ReadAllBytes(LiveDatabasePath));
        Assert.Equal(configBefore, File.ReadAllBytes(LiveConfigPath));
        Assert.DoesNotContain("Reload", appConfigurationManager.CallLog);
        AssertNoStagingLeftover();
        Assert.Empty(Directory.GetFiles(Path.Combine(_appRoot, "databases"), "*.restore-old-*"));
    }

    [Fact]
    public async Task HandleAsync_WhenReloadThrows_RollsBack_AndTheFakeDictionaryStaysPreRestore() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", """{"Theme":"Dark"}""");
        });
        var appConfigurationManager = new FakeAppConfigurationManager { ThrowOnReload = new IOException("boom") };
        var eventPublisher = new FakeEventPublisher();
        var configBefore = File.ReadAllBytes(LiveConfigPath);

        var result = await RunAsync(zipPath, appConfigurationManager: appConfigurationManager, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.RolledBack, result);
        AssertSingleFailureStartsWith(eventPublisher, "Swapping_Failure");
        Assert.Equal(configBefore, File.ReadAllBytes(LiveConfigPath));
        Assert.DoesNotContain("Reload", appConfigurationManager.CallLog);
    }

    [Fact]
    public async Task HandleAsync_WhenRollbackItselfFailsForOneMove_ReturnsRollbackIncomplete_WithTheLeftoverPathReported() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        SeedLiveDatabase(withMigrationHistory: false);
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, configuration: true, databaseMigrationId: null);
            AddDatabaseEntry(z, withMigrationHistory: false, migrationId: null);
            WriteRawEntry(z, "app.config", "{}");
        });
        var appConfigurationManager = new FakeAppConfigurationManager { ThrowOnReload = new IOException("boom") };
        var eventPublisher = new FakeEventPublisher();
        var handler = CreateHandler(appConfigurationManager: appConfigurationManager, eventPublisher: eventPublisher);

        var failedOnce = false;
        handler.OnBeforeRollbackMove = move => {
            if (move.Description == "database aside" && !failedOnce) {
                failedOnce = true;
                throw new IOException("simulated rollback failure");
            }
        };

        var result = await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), CancellationToken.None);

        Assert.Equal(RestoreOutcome.RollbackIncomplete, result);
        var failure = AssertSingleFailureStartsWith(eventPublisher, "RollbackIncomplete_Failure");
        Assert.Contains(".restore-old-", failure);
        var leftover = Directory.GetFiles(Path.Combine(_appRoot, "databases"), "*.restore-old-*").Single();
        Assert.True(File.Exists(leftover));
    }

    /// <summary>
    /// B15-style, but targeting the specific sidecar-reversal interaction (review finding): when
    /// the database's own main-file reversal fails, its -wal/-shm sidecars must stay exactly
    /// where they were moved aside to, not be moved back next to whatever is now at the live
    /// path (which, in this test, is nothing - the restored database's own reversal already ran
    /// and succeeded before "database aside" is reached in reverse order).
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenTheDatabaseAsideReversalFails_LeavesItsSidecarsWhereTheyWere() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        SeedLiveDatabase(withMigrationHistory: false);
        File.WriteAllText(LiveDatabasePath + "-wal", "live-wal-bytes");
        File.WriteAllText(LiveDatabasePath + "-shm", "live-shm-bytes");
        var zipPath = BuildZip(z => {
            AddManifest(z, database: true, configuration: true, databaseMigrationId: null);
            AddDatabaseEntry(z, withMigrationHistory: false, migrationId: null);
            WriteRawEntry(z, "app.config", "{}");
        });
        var appConfigurationManager = new FakeAppConfigurationManager { ThrowOnReload = new IOException("boom") };
        var eventPublisher = new FakeEventPublisher();
        var handler = CreateHandler(appConfigurationManager: appConfigurationManager, eventPublisher: eventPublisher);
        handler.OnBeforeRollbackMove = move => {
            if (move.Description == "database aside") {
                throw new IOException("simulated database-aside rollback failure");
            }
        };

        var result = await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), CancellationToken.None);

        Assert.Equal(RestoreOutcome.RollbackIncomplete, result);
        var failure = AssertSingleFailureStartsWith(eventPublisher, "RollbackIncomplete_Failure");

        var databaseDirectory = Path.Combine(_appRoot, "databases");
        var walAside = Directory.GetFiles(databaseDirectory, "*-wal.restore-old-*").Single();
        var shmAside = Directory.GetFiles(databaseDirectory, "*-shm.restore-old-*").Single();
        Assert.Contains(walAside, failure);
        Assert.Contains(shmAside, failure);
        // Never attempted: the sidecars stay exactly where the forward pass parked them.
        Assert.False(File.Exists(LiveDatabasePath + "-wal"));
        Assert.False(File.Exists(LiveDatabasePath + "-shm"));
    }

    [Fact]
    public async Task HandleAsync_WhenCloseAllThrowsAnAggregateException_RollsBack_WithNothingRenamedYet() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        var indexProvider = new FakeIndexProvider { ThrowOnCloseAll = new AggregateException(new InvalidOperationException("index busy")) };
        var eventPublisher = new FakeEventPublisher();
        var before = CaptureSnapshot();

        var result = await RunAsync(zipPath, indexProvider: indexProvider, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.RolledBack, result);
        AssertSingleFailureStartsWith(eventPublisher, "PreparingSwap_Failure");
        AssertSnapshotUnchanged(before);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequestedAfterTheCommitPointCheck_StillCompletes() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        using var cts = new CancellationTokenSource();
        var handler = CreateHandler();
        handler.OnBeforeMove = _ => cts.Cancel();

        var result = await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), cts.Token);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.Equal("{}", File.ReadAllText(LiveConfigPath));
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequestedBeforeTheCommitPointCheck_PropagatesCancellation() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        using var cts = new CancellationTokenSource();
        var eventPublisher = new FakeEventPublisher();
        var handler = CreateHandler(eventPublisher: eventPublisher);
        handler.OnBeforeCommitPointCheck = cts.Cancel;
        var configBefore = File.ReadAllBytes(LiveConfigPath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), cts.Token));

        Assert.DoesNotContain(
            eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>(),
            e => e.Type is NotificationType.Failure or NotificationType.Success);
        Assert.Equal(configBefore, File.ReadAllBytes(LiveConfigPath));
        AssertNoStagingLeftover();
    }

    /// <summary>
    /// Review finding: cancelling before <c>HandleAsync</c> even starts only proves <c>Task.Run</c>
    /// never starts the work - it says nothing about extraction itself observing the token.
    /// <see cref="PerformApplicationRestoreRequestHandler.OnAfterEntryExtracted"/> cancels only
    /// once the first of two entries has actually been extracted, so the second entry's
    /// <c>ThrowIfCancellationRequested</c> inside the extraction loop is what has to fire.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenCancelledMidExtraction_PropagatesCancellation_AfterExtractingTheFirstEntry() {
        var zipPath = BuildZip(z => {
            AddManifest(z, searchIndex: true);
            WriteRawEntry(z, "search-index/segment1.bin", "data-1");
            WriteRawEntry(z, "search-index/segment2.bin", "data-2");
        });
        using var cts = new CancellationTokenSource();
        var eventPublisher = new FakeEventPublisher();
        var handler = CreateHandler(eventPublisher: eventPublisher);
        var entriesExtracted = 0;
        handler.OnAfterEntryExtracted = () => {
            entriesExtracted++;
            cts.Cancel();
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), cts.Token));

        Assert.Equal(1, entriesExtracted); // proves the second entry's loop iteration never started
        Assert.DoesNotContain(
            eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>(),
            e => e.Type is NotificationType.Failure or NotificationType.Success);
        AssertNoStagingLeftover();
    }

    // ----- AC2/F3: round-trip with the real backup handler (B20) --------------------------------------

    [Fact]
    public async Task HandleAsync_RoundTripWithTheRealBackupHandler_RestoresTheOriginalFixtureBytes() {
        // No migration history: today's AppDbContext compiles zero migrations, so the expected
        // id is always null - the fixture must match that to pass the real D1 compatibility
        // check (this is today's actual reality per the plan's own context table, §1).
        SeedLiveConfig("""{"Theme":"Light","UILanguage":"en-GB"}""");
        SeedLiveDatabase(withMigrationHistory: false, markerValue: "fixture-row");
        SeedLiveIndex(("segment1.bin", "index-bytes-1"), ("segment2.bin", "index-bytes-2"));

        var originalConfigBytes = File.ReadAllBytes(LiveConfigPath);
        var originalDatabaseBytes = File.ReadAllBytes(LiveDatabasePath);
        var originalIndexBytes1 = File.ReadAllBytes(Path.Combine(LiveIndexDirectory, "segment1.bin"));
        var originalIndexBytes2 = File.ReadAllBytes(Path.Combine(LiveIndexDirectory, "segment2.bin"));

        var backupHandler = new PerformApplicationBackupRequestHandler(
            new FakeApplicationContext { Version = "v1.0.0", FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot) },
            new FakeAppConfigurationManager(),
            new FakeIndexProvider(),
            new PhysicalFileManagerFactory().GetOrCreate(_luceneRoot),
            Options.Create(new LuceneOptions { DirectoryRelativePath = LuceneRelativePath }),
            new FakeTimeProvider(),
            new FakeEventPublisher(),
            new FakeTranslator(),
            NullLogger<PerformApplicationBackupRequestHandler>.Instance);
        var backupResult = await backupHandler.HandleAsync(new PerformApplicationBackupRequest(), CancellationToken.None);
        Assert.True(backupResult.Succeeded);

        // Mutate the live state so the restore has something real to undo.
        File.WriteAllText(LiveConfigPath, """{"Theme":"Dark"}""");
        File.WriteAllText(Path.Combine(LiveIndexDirectory, "segment1.bin"), "mutated");

        var result = await RunAsync(backupResult.BackupFilePath!);

        Assert.Equal(RestoreOutcome.Completed, result);
        // Config and the index are plain file copies end to end, so they round-trip byte-for-byte.
        Assert.Equal(originalConfigBytes, File.ReadAllBytes(LiveConfigPath));
        Assert.Equal(originalIndexBytes1, File.ReadAllBytes(Path.Combine(LiveIndexDirectory, "segment1.bin")));
        Assert.Equal(originalIndexBytes2, File.ReadAllBytes(Path.Combine(LiveIndexDirectory, "segment2.bin")));

        // The database is NOT expected to be byte-identical: IPX-0009's own backup step copies
        // it through SQLite's page-level online-backup API and an extra PRAGMA statement, which
        // bumps the file change counter by one more transaction than the original fixture ever
        // had - a pre-existing, correct characteristic of the backup handler, not a restore bug.
        // Content equality is what actually matters here.
        _ = originalDatabaseBytes;
        using var connection = new SqliteConnection($"Data Source={LiveDatabasePath};Mode=ReadOnly;Pooling=false;");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Marker;";
        Assert.Equal("fixture-row", command.ExecuteScalar());
    }

    [Fact]
    public async Task HandleAsync_WhenPublishingTheSuccessNotificationThrows_StillCompletes() {
        SeedLiveConfig("""{"Theme":"Light"}""");
        var zipPath = BuildZip(z => {
            AddManifest(z, configuration: true);
            WriteRawEntry(z, "app.config", "{}");
        });
        var eventPublisher = new FakeEventPublisher();
        eventPublisher.OnPublish = @event => {
            if (@event is BackupRecoveryProgressNotificationEvent { Type: NotificationType.Success }) {
                throw new IOException("boom");
            }
        };

        var result = await RunAsync(zipPath, eventPublisher: eventPublisher);

        Assert.Equal(RestoreOutcome.Completed, result);
        Assert.Equal("{}", File.ReadAllText(LiveConfigPath));
    }

    // ----- helpers --------------------------------------------------------------------------------

    private async Task<RestoreOutcome> RunAsync(
        string zipPath,
        FakeIndexProvider? indexProvider = null,
        FakeAppConfigurationManager? appConfigurationManager = null,
        FakeEventPublisher? eventPublisher = null,
        string? expectedMigrationId = null) {
        var handler = CreateHandler(indexProvider, appConfigurationManager, eventPublisher, expectedMigrationId);

        return await handler.HandleAsync(new PerformApplicationRestoreRequest(zipPath), CancellationToken.None);
    }

    private PerformApplicationRestoreRequestHandler CreateHandler(
        FakeIndexProvider? indexProvider = null,
        FakeAppConfigurationManager? appConfigurationManager = null,
        FakeEventPublisher? eventPublisher = null,
        string? expectedMigrationId = null) {
        var applicationContext = new FakeApplicationContext {
            Version = "v1.0.0",
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_appRoot)
        };

        return new PerformApplicationRestoreRequestHandler(
            applicationContext,
            appConfigurationManager ?? new FakeAppConfigurationManager(),
            indexProvider ?? new FakeIndexProvider(),
            new PhysicalFileManagerFactory().GetOrCreate(_luceneRoot),
            Options.Create(new LuceneOptions { DirectoryRelativePath = LuceneRelativePath }),
            CreateDbContext(expectedMigrationId),
            eventPublisher ?? new FakeEventPublisher(),
            new PassthroughTranslator(),
            NullLogger<PerformApplicationRestoreRequestHandler>.Instance);
    }

    /// <summary>
    /// A real <see cref="AppDbContext"/> whose <c>Database.GetMigrations()</c> is empty (today's
    /// reality: no compiled migrations), unless <paramref name="expectedMigrationId"/> forces a
    /// particular expectation for the F3 round-trip test via a trivial migrator stand-in is not
    /// worth building - so that single test passes <see langword="null"/> through the manifest's
    /// own migration id instead, by seeding the fixture with that id and never changing it.
    /// </summary>
    private static AppDbContext CreateDbContext(string? expectedMigrationId) {
        _ = expectedMigrationId; // Documented limitation: see summary. Kept as a parameter for intent.
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;

        return new AppDbContext(options);
    }

    private void AssertNoStagingLeftover() {
        var temporaryDirectory = Path.Combine(_appRoot, "temporary");
        Assert.True(
            !Directory.Exists(temporaryDirectory) || Directory.GetDirectories(temporaryDirectory, "restore-*").Length == 0,
            "Expected no leftover 'restore-*' staging folder.");
    }

    private DataRootSnapshot CaptureSnapshot() {
        return DataRootSnapshot.Capture(_appRoot, _luceneRoot);
    }

    private void AssertSnapshotUnchanged(DataRootSnapshot before) {
        var after = CaptureSnapshot();
        Assert.Equal(before.Files, after.Files);
    }

    private static string AssertSingleFailureStartsWith(FakeEventPublisher eventPublisher, string keyPrefix) {
        var progressEvents = eventPublisher.PublishedEvents.OfType<BackupRecoveryProgressNotificationEvent>().ToList();
        var failure = Assert.Single(progressEvents, e => e.Type == NotificationType.Failure);
        Assert.DoesNotContain(progressEvents, e => e.Type == NotificationType.Success);
        Assert.True(
            failure.Message.StartsWith($"PerformApplicationRestoreRequestHandler_{keyPrefix}", StringComparison.Ordinal),
            $"Expected a failure starting with 'PerformApplicationRestoreRequestHandler_{keyPrefix}' but got '{failure.Message}'.");

        return failure.Message;
    }

    private void SeedLiveConfig(string json) {
        File.WriteAllText(LiveConfigPath, json);
    }

    /// <summary>
    /// Seeds the live database. <paramref name="markerValue"/> is a plain data value (not a
    /// migration id) a test can read back to prove which database ended up live - this app's
    /// <see cref="AppDbContext"/> compiles no migrations today, so the only way to be
    /// compatibility-checked as <c>null == null</c> is for every fixture DB in these tests to
    /// also have no (or a <see langword="null"/>) migration id.
    /// </summary>
    private void SeedLiveDatabase(bool withMigrationHistory, string? migrationId = null, string markerValue = "marker") {
        Directory.CreateDirectory(Path.Combine(_appRoot, "databases"));
        using var connection = new SqliteConnection($"Data Source={LiveDatabasePath};Pooling=false;");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE Marker (Value TEXT);
            INSERT INTO Marker (Value) VALUES ('{markerValue}');
            """ + (withMigrationHistory
            ? $"""
                CREATE TABLE __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT);
                INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{migrationId}', '10.0.0');
                """
            : "");
        command.ExecuteNonQuery();
    }

    private void SeedLiveIndex(params (string Name, string Content)[] files) {
        Directory.CreateDirectory(LiveIndexDirectory);
        foreach (var (name, content) in files) {
            File.WriteAllText(Path.Combine(LiveIndexDirectory, name), content);
        }
    }

    private static string BuildZip(Action<ZipArchive> configure) {
        var path = Path.Combine(Directory.CreateTempSubdirectory("ipx-0011-src-").FullName, "backup.zip");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        configure(archive);

        return path;
    }

    private static void AddManifest(
        ZipArchive archive,
        int formatVersion = 1,
        string application = "InfoPhoenix",
        string appVersion = "v1.0.0",
        string? databaseMigrationId = null,
        bool database = false,
        bool searchIndex = false,
        bool configuration = false) {
        var manifest = new BackupManifest(
            formatVersion, application, appVersion, databaseMigrationId, DateTimeOffset.UnixEpoch,
            new BackupManifestContents(database, searchIndex, configuration));
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        WriteRawEntry(archive, "backup-manifest.json", json);
    }

    private static void AddDatabaseEntry(
        ZipArchive archive, bool withMigrationHistory, string? migrationId, string markerValue = "marker") {
        var databasePath = Path.Combine(Directory.CreateTempSubdirectory("ipx-0011-db-").FullName, "app.db");
        using (var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false;")) {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"""
                CREATE TABLE Marker (Value TEXT);
                INSERT INTO Marker (Value) VALUES ('{markerValue}');
                """ + (withMigrationHistory
                ? $"""
                    CREATE TABLE __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT);
                    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{migrationId}', '10.0.0');
                    """
                : "");
            command.ExecuteNonQuery();
        }

        archive.CreateEntryFromFile(databasePath, "database/app.db");
    }

    private static void AddDatabaseEntryWithMigrationsHistoryAsView(ZipArchive archive) {
        var databasePath = Path.Combine(Directory.CreateTempSubdirectory("ipx-0011-db-").FullName, "app.db");
        using (var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false;")) {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE RealTable (MigrationId TEXT, ProductVersion TEXT);
                CREATE VIEW __EFMigrationsHistory AS SELECT MigrationId, ProductVersion FROM RealTable;
                """;
            command.ExecuteNonQuery();
        }

        archive.CreateEntryFromFile(databasePath, "database/app.db");
    }

    private static void WriteRawEntry(
        ZipArchive archive, string name, string content, CompressionLevel compressionLevel = CompressionLevel.Optimal) {
        var entry = archive.CreateEntry(name, compressionLevel);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Builds a real, valid zip with a Stored (uncompressed) search-index entry, then
    /// binary-patches the central-directory record's "uncompressed size" field for that entry
    /// down to a tiny value. Unlike a compressed entry - whose read stream the BCL truncates to
    /// the declared length regardless (verified directly against the BCL) - a Stored entry's
    /// bytes flow straight through, so this is the one way to make a lying declared size
    /// actually reach <c>ExtractEntryBounded</c>'s bounded-copy guard.
    /// </summary>
    private static string BuildZipWithLyingSizeEntry() {
        var zipPath = BuildZip(z => {
            AddManifest(z, searchIndex: true);
            WriteRawEntry(z, "search-index/segment1.bin", new string('x', 4096), CompressionLevel.NoCompression);
        });

        PatchDeclaredUncompressedSize(zipPath, "search-index/segment1.bin", newDeclaredSize: 10);

        return zipPath;
    }

    private static void PatchDeclaredUncompressedSize(string zipPath, string entryName, int newDeclaredSize) {
        var bytes = File.ReadAllBytes(zipPath);
        var nameBytes = Encoding.UTF8.GetBytes(entryName);

        // Central directory file header: signature(4) + 20 bytes of fixed fields before the
        // uncompressed-size field at relative offset 24, then the file name at offset 46.
        const uint centralDirectorySignature = 0x02014b50;
        var patched = false;

        for (var i = 0; i <= bytes.Length - 46 - nameBytes.Length; i++) {
            if (BitConverter.ToUInt32(bytes, i) != centralDirectorySignature) {
                continue;
            }

            var nameStart = i + 46;
            if (!bytes.AsSpan(nameStart, nameBytes.Length).SequenceEqual(nameBytes)) {
                continue;
            }

            Array.Copy(BitConverter.GetBytes(newDeclaredSize), 0, bytes, i + 24, 4);
            patched = true;
            break;
        }

        if (!patched) {
            throw new InvalidOperationException($"Could not find the central directory header for entry '{entryName}'.");
        }

        File.WriteAllBytes(zipPath, bytes);
    }

    /// <summary>Builds a Stored-entry zip, then flips one data byte without touching any header.</summary>
    private static string BuildZipWithTamperedEntry() {
        var zipPath = BuildZip(z => {
            AddManifest(z, searchIndex: true);
            WriteRawEntry(z, "search-index/segment1.bin", new string('x', 64), CompressionLevel.NoCompression);
        });

        TamperStoredEntryFirstByte(zipPath, "search-index/segment1.bin");

        return zipPath;
    }

    /// <summary>
    /// Flips the first data byte of a Stored local-file-header entry, leaving every header field
    /// (including the local CRC-32, which <see cref="ZipArchiveEntry.Crc32"/> reads from the
    /// central directory, not re-derived from the tampered bytes) exactly as it was.
    /// </summary>
    private static void TamperStoredEntryFirstByte(string zipPath, string entryName) {
        var bytes = File.ReadAllBytes(zipPath);
        var nameBytes = Encoding.UTF8.GetBytes(entryName);

        const uint localFileHeaderSignature = 0x04034b50;
        var tampered = false;

        for (var i = 0; i <= bytes.Length - 30 - nameBytes.Length; i++) {
            if (BitConverter.ToUInt32(bytes, i) != localFileHeaderSignature) {
                continue;
            }

            var nameStart = i + 30;
            if (!bytes.AsSpan(nameStart, nameBytes.Length).SequenceEqual(nameBytes)) {
                continue;
            }

            var extraFieldLength = BitConverter.ToUInt16(bytes, i + 28);
            var dataStart = nameStart + nameBytes.Length + extraFieldLength;
            bytes[dataStart] ^= 0xFF;
            tampered = true;
            break;
        }

        if (!tampered) {
            throw new InvalidOperationException($"Could not find the local file header for entry '{entryName}'.");
        }

        File.WriteAllBytes(zipPath, bytes);
    }

    private sealed class PassthroughTranslator : Nameless.Windows.Localization.ITranslator {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public System.Globalization.CultureInfo CurrentCulture { get; private set; } = System.Globalization.CultureInfo.InvariantCulture;

        public string this[string key] => key;

        public string this[string key, object[] args] => $"{key}: {string.Join(", ", args)}";

        public void SetCulture(System.Globalization.CultureInfo culture) {
            CurrentCulture = culture;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        }

        public void SetCulture(string isoCode) {
            SetCulture(System.Globalization.CultureInfo.GetCultureInfo(isoCode));
        }
    }
}
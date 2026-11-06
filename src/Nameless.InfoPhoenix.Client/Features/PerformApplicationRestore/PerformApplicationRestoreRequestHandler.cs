using System.IO;
using System.IO.Compression;
using System.IO.Hashing;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nameless.Application;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;
using Nameless.IO;
using Nameless.Lucene;
using Nameless.Mediator.Requests;
using Nameless.ObjectModel;
using Nameless.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Events;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.BackupRecovery;

namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;

/// <summary>
/// Validates a backup archive's compatibility and restores it atomically (plan IPX-0011): the
/// archive is validated and extracted to a per-run temp folder with nothing live touched
/// (AC1), and only on full success is the staged data swapped into place through journaled,
/// same-volume renames that are fully reversed on any in-commit failure (AC2/AC3).
/// </summary>
/// <remarks>
/// Follows the IPX-0008 handler contract for every step before the commit point: one
/// Information entry per step, no <c>ConfigureAwait(false)</c>/<c>SkipContextSync</c>. After the
/// commit point (plan D7 §3.2) the cancellation token is never observed again, by design.
/// </remarks>
public sealed class PerformApplicationRestoreRequestHandler
    : IRequestHandler<PerformApplicationRestoreRequest, RestoreOutcome> {
    private const string Prefix = nameof(PerformApplicationRestoreRequestHandler);

    private static readonly JsonSerializerOptions ManifestSerializerOptions = new(JsonSerializerDefaults.Web) {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase) {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        // Review finding: the superscript-digit variants (U+00B9/00B2/00B3) are also reserved by
        // Windows, not just the ASCII digits above.
        "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"
    };

    private readonly IApplicationContext _applicationContext;
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IIndexProvider _indexProvider;
    private readonly IFileManager _fileManager;
    private readonly LuceneOptions _luceneOptions;
    private readonly AppDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ITranslator _translator;
    private readonly ILogger<PerformApplicationRestoreRequestHandler> _logger;

    public PerformApplicationRestoreRequestHandler(
        IApplicationContext applicationContext,
        IAppConfigurationManager appConfigurationManager,
        IIndexProvider indexProvider,
        IFileManager fileManager,
        IOptions<LuceneOptions> luceneOptions,
        AppDbContext dbContext,
        IEventPublisher eventPublisher,
        ITranslator translator,
        ILogger<PerformApplicationRestoreRequestHandler> logger) {
        _applicationContext = Throws.When.Null(applicationContext);
        _appConfigurationManager = Throws.When.Null(appConfigurationManager);
        _indexProvider = Throws.When.Null(indexProvider);
        _fileManager = Throws.When.Null(fileManager);
        _luceneOptions = Throws.When.Null(luceneOptions).Value;
        _dbContext = Throws.When.Null(dbContext);
        _eventPublisher = Throws.When.Null(eventPublisher);
        _translator = Throws.When.Null(translator);
        _logger = Throws.When.Null(logger);
    }

    /// <summary>Test-only hook (plan §3.2), null (no-op) in production. See the backup handler.</summary>
    internal Action? OnBeforeCommitPointCheck { get; set; }

    /// <summary>
    /// Test-only hook invoked immediately before each forward commit move. Lets a test request
    /// cancellation from inside the commit to prove it is ignored (B18/plan rule 4).
    /// </summary>
    internal Action<PlannedMove>? OnBeforeMove { get; set; }

    /// <summary>
    /// Test-only hook invoked immediately before each reverse (rollback) move, letting a test
    /// make exactly one reversal fail without faking the OS (B15).
    /// </summary>
    internal Action<PlannedMove>? OnBeforeRollbackMove { get; set; }

    /// <summary>
    /// Test-only seam (null in production: real <see cref="DriveInfo.AvailableFreeSpace"/> is
    /// used) for the free-space preflight (plan D3/B5) - lets a test fail that check
    /// deterministically without depending on how much real disk space the test machine has.
    /// </summary>
    internal Func<string, long>? AvailableFreeSpaceOverride { get; set; }

    /// <summary>
    /// Test-only override (null in production: the real staging/install volume roots decide) for
    /// whether the search index needs a cross-volume copy (plan D5) - lets a test exercise that
    /// path deterministically without a real second drive.
    /// </summary>
    internal bool? ForceCrossVolumeIndexNeeded { get; set; }

    /// <summary>
    /// Test-only hook invoked immediately before the cross-volume index copy (plan D5), letting a
    /// test simulate a partial-copy failure (for example, disk full on the install volume)
    /// without faking the OS.
    /// </summary>
    internal Action? OnBeforeCrossVolumeCopy { get; set; }

    /// <summary>
    /// Test-only hook invoked immediately after each file is copied into the cross-volume
    /// sibling folder (plan D5), letting a test fail a copy strictly *after* the sibling folder
    /// already exists with at least one file in it - proving the leak-cleanup path actually
    /// sweeps a partially populated sibling, not just one that was never created.
    /// </summary>
    internal Action? OnAfterCrossVolumeFileCopied { get; set; }

    /// <summary>
    /// Test-only hook invoked right after each archive entry finishes extracting, letting a test
    /// request cancellation strictly between entries to prove extraction actually observes
    /// cancellation mid-loop (plan review finding), rather than only before the whole step starts.
    /// </summary>
    internal Action? OnAfterEntryExtracted { get; set; }

    /// <summary>One completed rename, recorded so rollback can undo it in reverse (plan D7).</summary>
    internal sealed record PlannedMove(string From, string To, string Description);

    private enum EntryKind { Manifest, Configuration, Database, SearchIndexFile, Ignored }

    private sealed record ArchiveEntryInfo(string Name, EntryKind Kind, string? RelativePath, long Length);

    public async Task<RestoreOutcome> HandleAsync(
        PerformApplicationRestoreRequest request, CancellationToken cancellationToken) {
        var step = "ReadingBackup";
        string? stagingDirectory = null;
        string? crossVolumeIndexSibling = null;
        var committed = false;
        var journal = new List<PlannedMove>();

        try {
            await PublishStepAsync(step);

            var (manifest, entries, _, versionWarningArgs) = await Task.Run(
                () => ReadBackup(request.SourceZipFilePath), cancellationToken);

            if (versionWarningArgs is not null) {
                await PublishWarningAsync("VersionDiffers_Warning", versionWarningArgs);
            }

            step = "ExtractingBackup";
            await PublishStepAsync(step);

            // Computed and captured to the cleanup variable *before* any IO (plan AC1): if
            // extraction fails after creating the folder, the outer `finally` must still know
            // its path, regardless of where inside ExtractBackup the failure happened.
            stagingDirectory = SysPath.Combine(
                _applicationContext.FileManager.GetTemporaryDirectory().Path,
                Constants.BackupRecovery.RestoreStagingDirectoryPrefix + Guid.NewGuid().ToString("N"));

            var crossVolumeNeeded = IsCrossVolumeIndexNeeded(manifest);
            var contentDirectory = await Task.Run(
                () => ExtractBackup(request.SourceZipFilePath, entries, crossVolumeNeeded, stagingDirectory, cancellationToken),
                cancellationToken);

            step = "VerifyingBackup";
            await PublishStepAsync(step);
            await Task.Run(() => VerifyBackup(contentDirectory, manifest), cancellationToken);

            step = "PreparingSwap";
            await PublishStepAsync(step);

            var runSuffix = Guid.NewGuid().ToString("N");

            // Computed and captured to the cleanup variable *before* PrepareSwap runs (same
            // reasoning as stagingDirectory above, plan AC1/review finding): a partial
            // cross-volume copy failure inside PrepareSwap must still leave the outer `finally`
            // able to find and sweep this path, instead of leaking it until the next run.
            if (crossVolumeNeeded) {
                crossVolumeIndexSibling = _fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath).Path +
                    Constants.BackupRecovery.RestoreNewSuffix + runSuffix;
            }

            var (plan, warnings) = await Task.Run(
                () => PrepareSwap(contentDirectory, manifest, runSuffix, crossVolumeIndexSibling), cancellationToken);

            foreach (var warningKey in warnings) {
                await PublishWarningAsync(warningKey);
            }

            _logger.LogInformation(
                "Restore commit plan for '{SourceZipFilePath}': {Moves}.",
                request.SourceZipFilePath, string.Join(" | ", plan.Select(m => $"{m.Description}: {m.From} => {m.To}")));

            OnBeforeCommitPointCheck?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            step = "Swapping";
            await PublishStepAsync(step);

            try {
                // Commit point (plan D7 §3.2): no token is passed from here on, by design - a
                // Cancel click during or after the swap must never interrupt or undo it.
                await Task.Run(() => ExecuteMoves(plan, journal));
                _appConfigurationManager.Reload();
                committed = true;
            }
            // Unfiltered catch (plan D7 rule): no token is in scope past the commit point, so
            // any exception here - including an unexpected OperationCanceledException - must
            // lead to rollback, never to propagation.
            catch (Exception ex) {
                var leftovers = RollbackMoves(journal);

                if (leftovers.Count > 0) {
                    _logger.LogError(
                        ex, "Restore rollback after a swap failure left data behind at: {Leftovers}.", string.Join("; ", leftovers));
                    await PublishFailureAsync("RollbackIncomplete_Failure", [ex.Message, string.Join("; ", leftovers)]);

                    return RestoreOutcome.RollbackIncomplete;
                }

                _logger.LogError(ex, "Restore failed during the swap; the previous data was put back.");
                await PublishFailureAsync("Swapping_Failure", [ex.Message]);

                return RestoreOutcome.RolledBack;
            }

            // Guarded (review finding): the swap already committed, so a failure publishing
            // these best-effort notifications must never be reported back as a failed restore -
            // every outer catch below is filtered `when (!committed)` and would otherwise never
            // run, letting the exception escape the handler entirely. Each publish gets its own
            // try/catch (review finding): a failure on one warning must never suppress the
            // remaining warnings or the success notification that follows it - the restore itself
            // already succeeded regardless of whether the user gets to see every notification.
            var cleanupFailures = DeleteAsideItems(journal);
            foreach (var leftoverPath in cleanupFailures) {
                try {
                    await PublishWarningAsync("CleanupIncomplete_Warning", [leftoverPath]);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Restore committed successfully, but a post-commit cleanup-warning notification failed.");
                }
            }

            try {
                await _eventPublisher.PublishSuccessAsync<BackupRecoveryProgressNotificationEvent>(
                    message: _translator[$"{Prefix}_Success"]);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Restore committed successfully, but the success notification failed.");
            }

            return RestoreOutcome.Completed;
        }
        catch (BackupRejectedException rejected) {
            _logger.LogWarning("Restore blocked: {ReasonKey}.", rejected.Rejection.ReasonKey);
            await PublishFailureAsync($"Rejected_{rejected.Rejection.ReasonKey}", rejected.Rejection.Args);

            return RestoreOutcome.Blocked;
        }
        catch (InvalidDataException ex) when (!committed) {
            _logger.LogWarning(ex, "Restore blocked: the archive is corrupted.");
            await PublishFailureAsync("Rejected_Corrupted", [ex.Message]);

            return RestoreOutcome.Blocked;
        }
        catch (Exception ex) when (!committed && ex is not OperationCanceledException) {
            _logger.LogError(ex, "Restore failed at step '{Step}'.", step);
            await PublishFailureAsync($"{step}_Failure", [ex.Message]);

            return RestoreOutcome.RolledBack;
        }
        finally {
            if (stagingDirectory is not null) {
                TryDeleteDirectory(stagingDirectory);
            }

            if (crossVolumeIndexSibling is not null) {
                TryDeleteDirectory(crossVolumeIndexSibling);
            }
        }
    }

    private Task PublishStepAsync(string stepName) {
        return _eventPublisher.PublishInformationAsync<BackupRecoveryProgressNotificationEvent>(
            message: _translator[$"{Prefix}_{stepName}"]);
    }

    private Task PublishWarningAsync(string key, object[]? args = null) {
        return _eventPublisher.PublishWarningAsync<BackupRecoveryProgressNotificationEvent>(
            message: args is null ? _translator[$"{Prefix}_{key}"] : _translator[$"{Prefix}_{key}", args]);
    }

    private Task PublishFailureAsync(string key, object[] args) {
        return _eventPublisher.PublishFailureAsync<BackupRecoveryProgressNotificationEvent>(
            message: _translator[$"{Prefix}_{key}", args]);
    }

    // ----- Step 1: ReadingBackup (plan D2) -----------------------------------------------------

    private (BackupManifest Manifest, List<ArchiveEntryInfo> Entries, string? ExpectedMigrationId, object[]? VersionWarningArgs)
        ReadBackup(string sourceZipFilePath) {
        GuardSelectedFilePath(sourceZipFilePath);

        ZipArchive archive;
        try {
            archive = ZipFile.OpenRead(sourceZipFilePath);
        }
        catch (InvalidDataException) {
            throw new BackupRejectedException(new BackupRejection("NotAnArchive"));
        }

        using (archive) {
            var rawEntries = archive.Entries;

            if (rawEntries.Count > Constants.BackupRecovery.MaxArchiveEntryCount) {
                throw new BackupRejectedException(new BackupRejection(
                    "TooManyEntries", [rawEntries.Count, Constants.BackupRecovery.MaxArchiveEntryCount]));
            }

            var entries = new List<ArchiveEntryInfo>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in rawEntries) {
                ValidateEntryName(entry.FullName);

                if (!seenNames.Add(entry.FullName)) {
                    throw new BackupRejectedException(new BackupRejection("UnexpectedEntry", [entry.FullName]));
                }

                var (kind, relativePath) = ClassifyEntry(entry.FullName);
                entries.Add(new ArchiveEntryInfo(entry.FullName, kind, relativePath, entry.Length));
            }

            var manifestInfo = entries.SingleOrDefault(e => e.Kind == EntryKind.Manifest)
                ?? throw new BackupRejectedException(new BackupRejection("ManifestMissing"));

            if (manifestInfo.Length > Constants.BackupRecovery.MaxManifestBytes) {
                throw new BackupRejectedException(new BackupRejection("ManifestInvalid"));
            }

            BackupManifest? manifest;
            try {
                var manifestEntry = archive.GetEntry(manifestInfo.Name)!;
                using var manifestStream = manifestEntry.Open();
                manifest = JsonSerializer.Deserialize<BackupManifest>(manifestStream, ManifestSerializerOptions);
            }
            catch (JsonException) {
                throw new BackupRejectedException(new BackupRejection("ManifestInvalid"));
            }

            if (manifest is null) {
                throw new BackupRejectedException(new BackupRejection("ManifestInvalid"));
            }

            var hasDatabaseEntry = entries.Any(e => e.Kind == EntryKind.Database);
            var hasConfigurationEntry = entries.Any(e => e.Kind == EntryKind.Configuration);
            var hasSearchIndexEntry = entries.Any(e => e.Kind == EntryKind.SearchIndexFile);

            if (manifest.Contents.Database != hasDatabaseEntry ||
                manifest.Contents.Configuration != hasConfigurationEntry ||
                manifest.Contents.SearchIndex != hasSearchIndexEntry) {
                throw new BackupRejectedException(new BackupRejection("ContentsMismatch"));
            }

            var expectedMigrationId = _dbContext.Database.GetMigrations().LastOrDefault();
            var rejection = BackupCompatibility.Evaluate(manifest, expectedMigrationId);
            if (rejection is not null) {
                throw new BackupRejectedException(rejection);
            }

            return (manifest, entries, expectedMigrationId, ComputeVersionWarningArgs(manifest));
        }
    }

    /// <summary>D1.4: a different app version alone never blocks - it is at most one Warning.</summary>
    private object[]? ComputeVersionWarningArgs(BackupManifest manifest) {
        var currentVersion = _applicationContext.Version;
        var sameVersion = SemVersion.TryParse(manifest.AppVersion, out var backupVersion) &&
            SemVersion.TryParse(currentVersion, out var runningVersion) &&
            backupVersion.Equals(runningVersion);

        return sameVersion ? null : [manifest.AppVersion, currentVersion];
    }

    /// <summary>
    /// D2 step 1: the selected file must not be something this run would sweep, move or lock -
    /// under <c>temporary\</c>, under the live Lucene directory, or equal to the live
    /// <c>app.config</c>/<c>app.db</c>.
    /// </summary>
    private void GuardSelectedFilePath(string sourceZipFilePath) {
        var fullSource = SysPath.GetFullPath(sourceZipFilePath);
        var temporaryDirectory = SysPath.GetFullPath(_applicationContext.FileManager.GetTemporaryDirectory().Path);
        var liveIndexDirectory = SysPath.GetFullPath(_fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath).Path);
        var configPath = SysPath.GetFullPath(
            _applicationContext.FileManager.GetFile(Constants.BackupRecovery.AppConfigurationFileName).Path);
        var databasePath = SysPath.GetFullPath(
            SysPath.Combine(_applicationContext.FileManager.GetDatabaseDirectory().Path, Constants.Database.FileName));

        if (IsUnderDirectory(fullSource, temporaryDirectory) ||
            IsUnderDirectory(fullSource, liveIndexDirectory) ||
            string.Equals(fullSource, configPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fullSource, databasePath, StringComparison.OrdinalIgnoreCase)) {
            throw new BackupRejectedException(new BackupRejection("SelectedFileInDataFolder"));
        }
    }

    private static bool IsUnderDirectory(string fullPath, string directoryFullPath) {
        var normalized = directoryFullPath.TrimEnd(SysPath.DirectorySeparatorChar) + SysPath.DirectorySeparatorChar;

        return fullPath.StartsWith(normalized, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>D3 name rules. Any violation gives <c>Rejected_UnexpectedEntry</c>.</summary>
    private static void ValidateEntryName(string name) {
        if (string.IsNullOrEmpty(name) || name.Length > 260 ||
            name.Contains('\\') || name.Contains(':') || name.Contains('\0') || name.StartsWith('/')) {
            throw Unexpected(name);
        }

        var segments = name.Split('/');
        for (var i = 0; i < segments.Length; i++) {
            var segment = segments[i];
            var isLastSegment = i == segments.Length - 1;

            if (segment.Length == 0) {
                // Only the trailing segment of a directory entry (a name ending in '/') may be empty.
                if (!isLastSegment) {
                    throw Unexpected(name);
                }

                continue;
            }

            if (segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ') ||
                segment.IndexOfAny(SysPath.GetInvalidFileNameChars()) >= 0) {
                throw Unexpected(name);
            }

            var dotIndex = segment.IndexOf('.');
            var baseName = dotIndex < 0 ? segment : segment[..dotIndex];

            if (ReservedDeviceNames.Contains(baseName)) {
                throw Unexpected(name);
            }
        }

        static BackupRejectedException Unexpected(string entryName) {
            return new BackupRejectedException(new BackupRejection("UnexpectedEntry", [entryName]));
        }
    }

    /// <summary>D3 allow-list. Anything else gives <c>Rejected_UnexpectedEntry</c>.</summary>
    private static (EntryKind Kind, string? RelativePath) ClassifyEntry(string name) {
        if (name == Constants.BackupRecovery.ManifestEntryName) {
            return (EntryKind.Manifest, null);
        }

        if (name == Constants.BackupRecovery.ConfigurationEntryName) {
            return (EntryKind.Configuration, Constants.BackupRecovery.ConfigurationEntryName);
        }

        if (name == "database/") {
            return (EntryKind.Ignored, null);
        }

        if (name == Constants.BackupRecovery.DatabaseEntryName) {
            return (EntryKind.Database, SysPath.Combine("database", Constants.Database.FileName));
        }

        const string searchIndexPrefix = Constants.BackupRecovery.SearchIndexEntryDirectory + "/";

        if (name == searchIndexPrefix) {
            return (EntryKind.Ignored, null);
        }

        if (name.StartsWith(searchIndexPrefix, StringComparison.Ordinal)) {
            var relative = name[searchIndexPrefix.Length..];

            if (relative.Length == 0 || relative.EndsWith('/')) {
                return (EntryKind.Ignored, null);
            }

            var mapped = SysPath.Combine(
                Constants.BackupRecovery.SearchIndexEntryDirectory, relative.Replace('/', SysPath.DirectorySeparatorChar));

            return (EntryKind.SearchIndexFile, mapped);
        }

        throw new BackupRejectedException(new BackupRejection("UnexpectedEntry", [name]));
    }

    // ----- Step 2: ExtractingBackup (plan D3/D4) ------------------------------------------------

    private bool IsCrossVolumeIndexNeeded(BackupManifest manifest) {
        if (!manifest.Contents.SearchIndex) {
            return false;
        }

        if (ForceCrossVolumeIndexNeeded.HasValue) {
            return ForceCrossVolumeIndexNeeded.Value;
        }

        var temporaryRoot = SysPath.GetPathRoot(_applicationContext.FileManager.GetTemporaryDirectory().Path);
        var liveIndexRoot = SysPath.GetPathRoot(_fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath).Path);

        return !string.Equals(temporaryRoot, liveIndexRoot, StringComparison.OrdinalIgnoreCase);
    }

    private string ExtractBackup(
        string sourceZipFilePath, List<ArchiveEntryInfo> entries, bool crossVolumeIndexNeeded, string stagingDirectory,
        CancellationToken cancellationToken) {
        var temporaryDirectory = _applicationContext.FileManager.GetTemporaryDirectory();
        temporaryDirectory.Create();

        SweepOrphanStagingFolders(temporaryDirectory.Path);

        var contentDirectory = SysPath.Combine(stagingDirectory, "content");

        var declaredBytes = entries
            .Where(e => e.Kind is EntryKind.Database or EntryKind.Configuration or EntryKind.SearchIndexFile)
            .Sum(e => e.Length);

        // Checked before the content folder is created (plan AC1/D3).
        var availableFreeSpace = AvailableFreeSpaceOverride?.Invoke(stagingDirectory)
            ?? new DriveInfo(SysPath.GetPathRoot(stagingDirectory)!).AvailableFreeSpace;
        if (declaredBytes + Constants.BackupRecovery.FreeSpaceSafetyMarginBytes > availableFreeSpace) {
            throw new BackupRejectedException(new BackupRejection("InsufficientSpace", [FormatBytes(declaredBytes)]));
        }

        if (crossVolumeIndexNeeded) {
            // Review finding: the cross-volume sibling copy (plan D5) lands on the install
            // volume, next to the live index - not on the staging volume checked above - so it
            // needs its own preflight, checked with the same seam/margin, before any extraction.
            var indexBytes = entries.Where(e => e.Kind == EntryKind.SearchIndexFile).Sum(e => e.Length);
            var installVolumeRoot = SysPath.GetPathRoot(_fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath).Path)!;
            var installVolumeFreeSpace = AvailableFreeSpaceOverride?.Invoke(installVolumeRoot)
                ?? new DriveInfo(installVolumeRoot).AvailableFreeSpace;
            if (indexBytes + Constants.BackupRecovery.FreeSpaceSafetyMarginBytes > installVolumeFreeSpace) {
                throw new BackupRejectedException(new BackupRejection("InsufficientSpace", [FormatBytes(indexBytes)]));
            }
        }

        Directory.CreateDirectory(contentDirectory);

        using var archive = ZipFile.OpenRead(sourceZipFilePath);
        var normalizedContentRoot = SysPath.GetFullPath(contentDirectory).TrimEnd(SysPath.DirectorySeparatorChar) +
            SysPath.DirectorySeparatorChar;

        foreach (var entryInfo in entries) {
            // Checked once per entry (plan review finding): extraction previously never observed
            // the token mid-loop, so a large backup could not actually be cancelled until it had
            // already fully extracted.
            cancellationToken.ThrowIfCancellationRequested();

            if (entryInfo.RelativePath is null) {
                continue; // The manifest (read-only) and ignored directory entries are never extracted.
            }

            var destinationPath = SysPath.Combine(contentDirectory, entryInfo.RelativePath);
            var fullDestination = SysPath.GetFullPath(destinationPath);

            // Belt-and-braces zip-slip check (plan D3): the name rules above should make this
            // unreachable, which is the point of checking it again at the actual write site.
            if (!fullDestination.StartsWith(normalizedContentRoot, StringComparison.OrdinalIgnoreCase)) {
                throw new IOException($"Entry '{entryInfo.Name}' escapes the staging content folder.");
            }

            var entry = archive.GetEntry(entryInfo.Name)
                ?? throw new InvalidDataException($"Entry '{entryInfo.Name}' could not be reopened for extraction.");

            if (entryInfo.Kind == EntryKind.Configuration && entry.Length > Constants.BackupRecovery.MaxConfigurationBytes) {
                throw new InvalidDataException(
                    $"Entry '{entryInfo.Name}' declares {entry.Length} bytes, over the configuration size limit.");
            }

            ExtractEntryBounded(entry, destinationPath, cancellationToken);
            OnAfterEntryExtracted?.Invoke();
        }

        return contentDirectory;
    }

    /// <summary>
    /// Copies at most the entry's own declared <see cref="ZipArchiveEntry.Length"/> (plan D3's
    /// "do not trust declared sizes"): the loop aborts with <see cref="InvalidDataException"/>
    /// the instant more bytes arrive than the header promised, so a lying header cannot fill the
    /// disk. After the copy, both the total byte count and a CRC-32 computed over the copied
    /// bytes must match the entry's declared values (plan D3's "CRC-failing entries throw"),
    /// catching a short read or tampered/corrupt content that a Stored-compression entry would
    /// otherwise let straight through uncomplained. Checked once per buffer read, so a large
    /// entry's extraction can actually be cancelled mid-copy, not only before it starts. The
    /// file is flushed to disk before returning (plan D3's power-loss guarantee).
    /// </summary>
    private static void ExtractEntryBounded(ZipArchiveEntry entry, string destinationPath, CancellationToken cancellationToken) {
        Directory.CreateDirectory(SysPath.GetDirectoryName(destinationPath)!);

        using var source = entry.Open();
        using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        var crc = new Crc32();
        var buffer = new byte[81920];
        long totalWritten = 0;
        int read;

        while ((read = source.Read(buffer, 0, buffer.Length)) > 0) {
            cancellationToken.ThrowIfCancellationRequested();

            totalWritten += read;

            if (totalWritten > entry.Length) {
                throw new InvalidDataException(
                    $"Entry '{entry.FullName}' contains more data than its declared length of {entry.Length} bytes.");
            }

            crc.Append(buffer.AsSpan(0, read));
            destination.Write(buffer, 0, read);
        }

        if (totalWritten != entry.Length) {
            throw new InvalidDataException(
                $"Entry '{entry.FullName}' extracted {totalWritten} bytes, short of its declared length of {entry.Length} bytes.");
        }

        if (crc.GetCurrentHashAsUInt32() != entry.Crc32) {
            throw new InvalidDataException($"Entry '{entry.FullName}' failed its CRC-32 check.");
        }

        destination.Flush(flushToDisk: true);
    }

    private static string FormatBytes(long bytes) {
        return $"{bytes / 1024.0 / 1024.0:F1} MiB";
    }

    /// <summary>
    /// Best-effort delete of orphaned staging folders from a crashed previous run (plan D4).
    /// Never sweeps <c>*.restore-old-*</c> siblings: after a crash mid-commit, those may be the
    /// only remaining copy of the user's previous data.
    /// </summary>
    private void SweepOrphanStagingFolders(string temporaryDirectoryPath) {
        try {
            foreach (var directory in Directory.EnumerateDirectories(
                temporaryDirectoryPath, $"{Constants.BackupRecovery.RestoreStagingDirectoryPrefix}*")) {
                TryDeleteDirectory(directory);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to sweep orphan restore staging folders under '{TemporaryDirectory}'.", temporaryDirectoryPath);
        }

        try {
            var liveIndexDirectory = _fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath).Path.TrimEnd(SysPath.DirectorySeparatorChar);
            var luceneParent = SysPath.GetDirectoryName(liveIndexDirectory);

            if (luceneParent is not null && Directory.Exists(luceneParent)) {
                foreach (var directory in Directory.EnumerateDirectories(luceneParent, $"*{Constants.BackupRecovery.RestoreNewSuffix}*")) {
                    TryDeleteDirectory(directory);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to sweep orphan cross-volume index siblings.");
        }
    }

    private void TryDeleteDirectory(string path) {
        try {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to delete '{Path}'.", path);
        }
    }

    // ----- Step 3: VerifyingBackup (plan D9/§3.2) ------------------------------------------------

    private void VerifyBackup(string contentDirectory, BackupManifest manifest) {
        if (manifest.Contents.Configuration) {
            VerifyConfiguration(SysPath.Combine(contentDirectory, "app.config"));
        }

        if (manifest.Contents.Database) {
            VerifyDatabase(SysPath.Combine(contentDirectory, "database", Constants.Database.FileName), manifest);
        }
    }

    private static void VerifyConfiguration(string configurationPath) {
        try {
            using var document = JsonDocument.Parse(File.ReadAllBytes(configurationPath));

            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                throw new BackupRejectedException(new BackupRejection("ConfigurationInvalid"));
            }
        }
        catch (JsonException) {
            throw new BackupRejectedException(new BackupRejection("ConfigurationInvalid"));
        }
    }

    private static void VerifyDatabase(string databasePath, BackupManifest manifest) {
        var connectionString = new SqliteConnectionStringBuilder {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();

        try {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using (var trustedSchemaCommand = connection.CreateCommand()) {
                trustedSchemaCommand.CommandText = "PRAGMA trusted_schema=OFF;";
                trustedSchemaCommand.ExecuteNonQuery();
            }

            using (var quickCheckCommand = connection.CreateCommand()) {
                quickCheckCommand.CommandText = "PRAGMA quick_check;";
                var result = quickCheckCommand.ExecuteScalar() as string;

                if (!string.Equals(result, "ok", StringComparison.Ordinal)) {
                    throw new BackupRejectedException(new BackupRejection("DatabaseInvalid", [result ?? "corrupt"]));
                }
            }

            string? schemaType;
            using (var schemaCommand = connection.CreateCommand()) {
                schemaCommand.CommandText = "SELECT type FROM sqlite_master WHERE name = '__EFMigrationsHistory';";
                schemaType = schemaCommand.ExecuteScalar() as string;
            }

            // Untrusted manifest: a crafted VIEW could run arbitrary SQL functions, so this is
            // rejected outright rather than silently treated as "no migrations" (plan §7).
            if (schemaType is not null && !string.Equals(schemaType, "table", StringComparison.OrdinalIgnoreCase)) {
                throw new BackupRejectedException(
                    new BackupRejection("DatabaseInvalid", ["__EFMigrationsHistory exists but is not a table."]));
            }

            var migrationId = schemaType == "table" ? MigrationHistoryReader.ReadLatestMigrationId(connection) : null;

            if (!string.Equals(migrationId, manifest.DatabaseMigrationId, StringComparison.Ordinal)) {
                throw new BackupRejectedException(new BackupRejection("ContentsMismatch"));
            }
        }
        catch (SqliteException ex) {
            throw new BackupRejectedException(new BackupRejection("DatabaseInvalid", [ex.Message]));
        }
    }

    // ----- Step 4: PreparingSwap (plan D5/D6/D8/D11) --------------------------------------------

    private (List<PlannedMove> Plan, List<string> Warnings) PrepareSwap(
        string contentDirectory, BackupManifest manifest, string runSuffix, string? crossVolumeIndexSibling) {
        // D8: release every live handle before anything is renamed. A fresh connection or index
        // reopens lazily against the restored files once the swap commits.
        _indexProvider.CloseAll();
        SqliteConnection.ClearAllPools();

        var warnings = new List<string>();
        var plan = new List<PlannedMove>();

        BuildDatabaseMoves(contentDirectory, manifest, runSuffix, plan, warnings);
        BuildSearchIndexMoves(contentDirectory, manifest, runSuffix, crossVolumeIndexSibling, plan, warnings);
        BuildConfigurationMoves(contentDirectory, manifest, runSuffix, plan, warnings);

        // D5: every operation after the commit point must be a same-volume rename.
        foreach (var move in plan) {
            if (!string.Equals(SysPath.GetPathRoot(move.From), SysPath.GetPathRoot(move.To), StringComparison.OrdinalIgnoreCase)) {
                throw new IOException($"Planned move '{move.Description}' from '{move.From}' to '{move.To}' crosses volumes.");
            }
        }

        return (plan, warnings);
    }

    private void BuildDatabaseMoves(
        string contentDirectory, BackupManifest manifest, string runSuffix, List<PlannedMove> plan, List<string> warnings) {
        var databaseDirectory = _applicationContext.FileManager.GetDatabaseDirectory();
        Directory.CreateDirectory(databaseDirectory.Path);

        var liveDatabasePath = SysPath.Combine(databaseDirectory.Path, Constants.Database.FileName);
        var liveDatabaseExists = File.Exists(liveDatabasePath);

        if (!manifest.Contents.Database && liveDatabaseExists) {
            warnings.Add("DatabaseAbsent_Warning");
        }

        foreach (var suffix in Constants.BackupRecovery.DatabaseSidecarSuffixes) {
            var sidecarPath = liveDatabasePath + suffix;

            if (File.Exists(sidecarPath)) {
                plan.Add(new PlannedMove(
                    sidecarPath, sidecarPath + Constants.BackupRecovery.RestoreOldSuffix + runSuffix, $"database sidecar ({suffix})"));
            }
        }

        if (liveDatabaseExists) {
            plan.Add(new PlannedMove(
                liveDatabasePath, liveDatabasePath + Constants.BackupRecovery.RestoreOldSuffix + runSuffix, "database aside"));
        }

        if (manifest.Contents.Database) {
            var stagedDatabasePath = SysPath.Combine(contentDirectory, "database", Constants.Database.FileName);
            plan.Add(new PlannedMove(stagedDatabasePath, liveDatabasePath, "database restore"));
        }
    }

    private void BuildSearchIndexMoves(
        string contentDirectory, BackupManifest manifest, string runSuffix, string? crossVolumeIndexSibling,
        List<PlannedMove> plan, List<string> warnings) {
        var liveIndexDirectory = _fileManager.GetDirectory(_luceneOptions.DirectoryRelativePath);
        var liveIndexExists = liveIndexDirectory.Exists;

        if (!manifest.Contents.SearchIndex && liveIndexExists) {
            warnings.Add("SearchIndexAbsent_Warning");
        }

        var liveIndexParent = SysPath.GetDirectoryName(liveIndexDirectory.Path.TrimEnd(SysPath.DirectorySeparatorChar));
        if (liveIndexParent is not null) {
            Directory.CreateDirectory(liveIndexParent);
        }

        if (liveIndexExists) {
            plan.Add(new PlannedMove(
                liveIndexDirectory.Path, liveIndexDirectory.Path + Constants.BackupRecovery.RestoreOldSuffix + runSuffix, "index aside"));
        }

        if (manifest.Contents.SearchIndex) {
            var stagedIndexDirectory = SysPath.Combine(contentDirectory, Constants.BackupRecovery.SearchIndexEntryDirectory);
            string indexSource;

            if (crossVolumeIndexSibling is not null) {
                // D5: the index is the one artifact that may land on a different volume. Copy it
                // next to its live target first (still pre-commit, cancellable), then rename from
                // there, so the commit-phase move is a same-volume rename like every other. The
                // sibling path itself was already computed by the caller (plan AC1/review
                // finding), so a copy failure here still leaves it reachable for cleanup.
                OnBeforeCrossVolumeCopy?.Invoke();
                CopyDirectoryRecursive(stagedIndexDirectory, crossVolumeIndexSibling);
                indexSource = crossVolumeIndexSibling;
            }
            else {
                indexSource = stagedIndexDirectory;
            }

            plan.Add(new PlannedMove(indexSource, liveIndexDirectory.Path, "index restore"));
        }
    }

    private void BuildConfigurationMoves(
        string contentDirectory, BackupManifest manifest, string runSuffix, List<PlannedMove> plan, List<string> warnings) {
        var liveConfigFile = _applicationContext.FileManager.GetFile(Constants.BackupRecovery.AppConfigurationFileName);
        var liveConfigExists = liveConfigFile.Exists;

        if (!manifest.Contents.Configuration && liveConfigExists) {
            warnings.Add("ConfigurationAbsent_Warning");
        }

        if (liveConfigExists) {
            plan.Add(new PlannedMove(
                liveConfigFile.Path, liveConfigFile.Path + Constants.BackupRecovery.RestoreOldSuffix + runSuffix, "configuration aside"));
        }

        if (manifest.Contents.Configuration) {
            var stagedConfigPath = SysPath.Combine(contentDirectory, "app.config");
            plan.Add(new PlannedMove(stagedConfigPath, liveConfigFile.Path, "configuration restore"));
        }
    }

    private void CopyDirectoryRecursive(string sourceDirectory, string destinationDirectory) {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var filePath in Directory.EnumerateFiles(sourceDirectory)) {
            File.Copy(filePath, SysPath.Combine(destinationDirectory, SysPath.GetFileName(filePath)), overwrite: false);
            OnAfterCrossVolumeFileCopied?.Invoke();
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(sourceDirectory)) {
            CopyDirectoryRecursive(directoryPath, SysPath.Combine(destinationDirectory, SysPath.GetFileName(directoryPath)));
        }
    }

    // ----- Step 5: Swapping - commit and rollback (plan D6/D7) -----------------------------------

    private void ExecuteMoves(List<PlannedMove> plan, List<PlannedMove> journal) {
        foreach (var move in plan) {
            OnBeforeMove?.Invoke(move);

            if (Directory.Exists(move.From)) {
                Directory.Move(move.From, move.To);
            }
            else {
                File.Move(move.From, move.To, overwrite: false);
            }

            journal.Add(move);
        }
    }

    /// <summary>
    /// Undoes the journal in reverse, using renames only (plan D7): no deletes, no copies, no
    /// new disk space, so rollback cannot fail for the reasons the forward pass might. A step
    /// that itself fails is logged and skipped, and every remaining step still runs; the paths
    /// that could not be put back are returned.
    /// </summary>
    /// <remarks>
    /// Review finding: a "database aside" reversal failure must suppress the sidecar
    /// (-wal/-shm/-journal) reversals that come right after it in this reverse order - they
    /// moved aside, pre-commit, before the main file did (plan D6), so reversing them back onto
    /// whichever database file now ends up at the live path (the new one, if its own reversal
    /// also failed) risks SQLite replaying a stale WAL into the wrong database. The sidecars stay
    /// exactly where they are; the original data is safe there either way.
    /// </remarks>
    private List<string> RollbackMoves(List<PlannedMove> journal) {
        var leftovers = new List<string>();
        var skipDatabaseSidecars = false;

        for (var i = journal.Count - 1; i >= 0; i--) {
            var move = journal[i];

            if (skipDatabaseSidecars && move.Description.StartsWith("database sidecar", StringComparison.Ordinal)) {
                leftovers.Add(move.To);
                continue;
            }

            try {
                OnBeforeRollbackMove?.Invoke(move);

                if (Directory.Exists(move.To)) {
                    Directory.Move(move.To, move.From);
                }
                else {
                    File.Move(move.To, move.From, overwrite: false);
                }
            }
            // Unfiltered: at this point we are already reversing a failure with no token in
            // scope, and a rollback step's own failure must never abort the remaining reversals.
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to reverse the move '{Description}' from '{To}' back to '{From}'.", move.Description, move.To, move.From);
                leftovers.Add(move.To);

                if (move.Description == "database aside") {
                    skipDatabaseSidecars = true;
                }
            }
        }

        return leftovers;
    }

    // ----- Step 6: Success (post-commit, best-effort only) ---------------------------------------

    private List<string> DeleteAsideItems(List<PlannedMove> journal) {
        var failures = new List<string>();

        foreach (var move in journal) {
            if (!move.To.Contains(Constants.BackupRecovery.RestoreOldSuffix, StringComparison.Ordinal)) {
                continue;
            }

            try {
                if (Directory.Exists(move.To)) {
                    Directory.Delete(move.To, recursive: true);
                }
                else if (File.Exists(move.To)) {
                    File.Delete(move.To);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                _logger.LogWarning(ex, "Failed to delete the previous-data copy at '{Path}' after a successful restore.", move.To);
                failures.Add(move.To);
            }
        }

        return failures;
    }
}

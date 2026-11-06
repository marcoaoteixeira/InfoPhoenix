# IPX-0011: Validate compatibility and perform an atomic restore

- Ticket: [IPX-0011 (#27)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/27). This is the highest-risk ticket in the epic, because it carries the atomicity guarantee.
- Epic: [IPX-0007 Backup & Restore (#23)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23), decisions D13 (a mismatch blocks, no migration), D14 (prompt to relaunch, never automatic) and D15 (stage to a temp location, swap only on full success).
- Binding contracts:
  - [IPX-0008](IPX-0008-progress-error-surfacing-mechanism.md) sections 3.1-3.3. Deviations and refinements are listed in section 9.
  - [IPX-0009](IPX-0009-create-application-backup.md) D3/D4. Checked against the real `BackupManifest.cs` and `PerformApplicationBackupRequestHandler.cs`.
  - [IPX-0010](IPX-0010-select-backup-and-confirm-overwrite.md) D5 and the section 8 hand-off items (a) to (d).
- Stack: .NET 10 / WPF (`dotnet`), one project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**.
- UX spec: **not needed** (section 11).
- ADR: **none** (section 12).
- **Decided OD1** (section 10): the compatibility rule is **schema only** (option B, decided by the user on 2026-11-06). D11 (mirror semantics) and D7 (crash window accepted, startup recovery as a follow-up) are also approved as designed. No open decisions remain.

## 1. Context: what exists today (verified in this worktree, in Nameless.Windows/Common 4.0.0 and in the local Nameless source)

| Concern | Reality | Consequence for restore |
|---|---|---|
| Data root | `IApplicationContext.FileManager.Root` = `C:\ProgramData\InfoPhoenix\`. It holds `app.config`, `databases\app.db`, `backups\` and `temporary\`. | `backups\` and `temporary\` live next to the artifacts, so the whole root **cannot** be swapped as one directory (section 2.4). |
| Config | `{root}\app.config`. The location is fixed by a private constant in `AppConfigurationManager`, which also writes `app.config.tmp` and then renames it. `IAppConfigurationManager.Reload()` exists in 4.0.0 and re-reads the file into the in-memory dictionary. | **The in-memory dictionary outlives the file.** `App.OnCurrentHostStop` calls `SaveChangesAsync` on every exit. Without a `Reload()` after the swap, exiting the app **writes the pre-restore settings back over the restored `app.config`**. This is the biggest hidden trap in this ticket (D9). |
| DB | `{root}\databases\app.db`, `Pooling=false`. `AppDbContext` has no entities and no migrations, and nothing opens the DB today. | No live handle exists today. The sidecars (`-wal`, `-shm`, `-journal`) must move together with `app.db` (D6). |
| Lucene | `{DI IFileManager.Root}\databases\lucene\<indexName>\`. The DI `IFileManager` is rooted at `AppContext.BaseDirectory` (the install folder), not the data root. This is IPX-0009 R1, still unfixed. `IIndexProvider.CloseAll()` disposes every index and releases `write.lock`, and indexes reopen lazily. No index is ever written today. | The index may be on a **different volume** from `temporary\`, and under `Program Files` it may be read-only (D5, R2). |
| App version | `IApplicationContext.Version` = `SemVersion.Format(includePrefix: true)` of `AssemblyInformationalVersion`. That string **keeps the `+build` metadata**, and by default the SDK appends the commit SHA (`v1.0.0+aff14b3...`). | Comparing the raw strings would treat two builds of the same release as different versions. Any version comparison must go through `SemVersion` equality, which ignores build metadata (D1.4). |
| Archive (IPX-0009 actual code) | Entries `backup-manifest.json`, `app.config`, `database/app.db`, `search-index/...` (the contents of the `lucene` folder, `write.lock` skipped, reparse points skipped), built with `ZipFile.CreateFromDirectoryAsync(..., includeBaseDirectory: false)`. The manifest is camelCase (`JsonSerializerDefaults.Web`). `databaseMigrationId` is read from the staged copy (newest `MigrationId`, or `null`). The DB is normalized to `journal_mode=DELETE`. | The restore allow-list (D3) is exactly this set. F3 (round-trip test) guards it. |
| Today's backups | `contents = { database: false, searchIndex: false, configuration: true }` and `databaseMigrationId: null` on every install. | On today's installs a real restore swaps only `app.config`. Tests must build DB and index fixtures themselves (as IPX-0009 does). |
| VM seam | `AppConfigurationPageViewModel.PerformApplicationRestoreAsync`, after `ConfirmOverwrite` returns true: the `LogInformation` + `NotYetAvailable` snackbar pair. | This ticket replaces exactly those two statements (section 3.4). |
| TaskRunner | A delegate that **returns normally** after Cancel was pressed yields `TaskRunnerOutcome.Completed`. Only an OCE for the run's token yields `Cancelled` (`TaskRunnerWindowViewModel` source). | Ignoring the token after the commit point is safe, because a late Cancel cannot turn a committed restore into `Cancelled`. |
| Sample | `Nameless.WinApp` `PerformApplicationRestoreRequestHandler`: extract with `ZipFile.ExtractToDirectoryAsync`, swap per artifact through `.bak-*` siblings, `Reload()` before the commit flag, roll back in reverse. Its VM **relaunches the process on Yes**. | We keep its swap and rollback shape. We do not take its unvalidated `ExtractToDirectory` (no allow-list, no size bound), its snackbar-from-handler, its "cancel = RolledBack" behaviour, or its auto-relaunch (ticket out of scope). See section 9. |

## 2. Decisions

### D1: The compatibility rule: schema only (OD1, decided)

This is the decided rule (OD1 option B, "schema only"; section 10). The archive is rejected (outcome `Blocked`) unless **all** of checks 1-3 hold. They are checked in this order, and the first failure wins:

1. `formatVersion == Constants.BackupRecovery.FormatVersion` (1). This is the archive layout. Failure: `Rejected_UnsupportedFormat {0}=manifest.formatVersion`.
2. `application == Constants.Application.Name` (`"InfoPhoenix"`, the constant IPX-0009 writes; ordinal). Failure: `Rejected_WrongApplication {0}=manifest.application`.
3. **Schema:** only when `contents.database` is `true`, `manifest.databaseMigrationId` must equal the running app's **expected** migration ID, compared ordinally and with `null == null` counting as equal. The expected ID is `_dbContext.Database.GetMigrations().LastOrDefault()`, which reads the migrations compiled into the app and opens no connection. Failure: `Rejected_SchemaMismatch {0}=manifest.databaseMigrationId {1}=expectedMigrationId` (`null` rendered as `-`). When `contents.database` is `false`, no DB is swapped in, so the schema check is skipped, whatever `databaseMigrationId` says.
4. **App version: never blocks.** A different app version **alone** does not reject a backup. Compare `manifest.appVersion` with `IApplicationContext.Version` through `SemVersion` equality (build metadata ignored). When they differ, the handler publishes exactly one Warning (`VersionDiffers_Warning {0}=manifest.appVersion {1}=running version`) and continues. If either string does not parse as `SemVersion`, treat it as "differs" (one Warning, still no block). Same version with different `+build` metadata gives no Warning.

Nothing else blocks on compatibility grounds: no app-version equality, no Major.Minor range, no index-format check (none exists yet; a future index-format change must add its own marker, section 10).

Why the running app's **expected** migration and not the live DB's applied one: the live DB may not exist, and what matters is whether the code about to run against the restored file understands its schema.

Checks 1-3 live in one pure static function:

```csharp
internal static class BackupCompatibility {
    // Returns null when compatible, otherwise the rejection (key suffix + args).
    // Checks, in order: formatVersion, application, schema (only when manifest.Contents.Database).
    public static BackupRejection? Evaluate(BackupManifest manifest, string? expectedMigrationId);
}
```

The version warning (check 4) is computed separately in the handler, after `Evaluate` returns `null`, because it never rejects anything.

### D2: Read the manifest from the zip **before** writing anything to disk

Steps `ReadingBackup` (section 3.2):

1. **Path guard.** The selected file must not be under `{root}\temporary\`, under the live Lucene directory, or equal to `app.config` or `app.db`. Otherwise reject with `Rejected_SelectedFileInDataFolder`. Such a file would be swept, moved or locked by this run.
2. `using var archive = ZipFile.OpenRead(path)` inside `Task.Run`. This reads **only the central directory**. An `InvalidDataException` here means "not a zip" and the result is `Rejected_NotAnArchive`.
3. **Entry validation, which needs no decompression** (D3): allow-list, name safety, duplicates, entry count.
4. **Manifest.** Get the entry `backup-manifest.json`. If it is missing, reject with `Rejected_ManifestMissing`. If its `Length` is over `MaxManifestBytes` (64 KiB), reject with `Rejected_ManifestInvalid`. Deserialize from the entry stream with `new JsonSerializerOptions(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true }`, so that missing or null `application`, `appVersion`, `createdAtUtc` or `contents` throw `JsonException`. Any `JsonException` or `null` result gives `Rejected_ManifestInvalid`. Unknown members stay ignored (the default), as IPX-0009 D3 requires.
5. **Consistency between the `contents` flags and the entries**, in both directions. `database` ⇔ `database/app.db` is present. `configuration` ⇔ `app.config` is present. `searchIndex` ⇔ at least one **file** entry under `search-index/`. Any disagreement gives `Rejected_ContentsMismatch`.
6. Run `BackupCompatibility.Evaluate` (D1).

**AC1 is guaranteed structurally:** every check above runs before the first write anywhere. That includes the orphan sweep and the creation of the staging folder (D4). A mismatched or invalid archive causes **zero filesystem mutations**. Test T1 asserts this with a full tree snapshot (F4).

### D3: Treat the zip as untrusted input (zip-slip, path tricks, zip bombs)

A user can pick **any** `.zip` (IPX-0010 D3/D6). This ticket therefore does **not** use `ZipFile.ExtractToDirectory`. It extracts entry by entry through an allow-list:

| Entry name | Accepted as | Max size |
|---|---|---|
| `backup-manifest.json` | read only, never extracted | 64 KiB |
| `app.config` | `content\app.config` | 1 MiB (`MaxConfigurationBytes`) |
| `database/` (directory entry) | ignored | n/a |
| `database/app.db` | `content\database\app.db` | free space (below) |
| `search-index/` and `search-index/<rel>/` (directory entries) | `content\search-index\<rel>\` created | n/a |
| `search-index/<rel>` (file) | `content\search-index\<rel>` | free space |
| anything else | **reject** with `Rejected_UnexpectedEntry {0}=name` | n/a |

Name rules, all checked **before** any extraction. A violation gives `Rejected_UnexpectedEntry`:

- The name is not empty, is at most 260 characters, and contains no `\`, `:` (NTFS alternate data streams, for example `app.config:x`, and drive letters) or `\0`. It does not start with `/`.
- Split on `/`. Only a directory entry may have an empty last segment. No segment is empty, `.` or `..`. No segment ends with `.` or a space, because Windows trims those and `app.config.` would alias `app.config`. No segment contains `Path.GetInvalidFileNameChars()`, and no segment is a reserved device name (`CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9`, with or without an extension).
- No duplicate names, compared **case-insensitively** (the NTFS semantics). This removes "which copy wins" ambiguity.
- At most `MaxArchiveEntryCount` (10,000) entries.
- **Belt and braces (zip-slip):** when extracting, `Path.GetFullPath(Path.Combine(contentRoot, mappedRelativePath))` must start with `contentRoot + Path.DirectorySeparatorChar` (`OrdinalIgnoreCase`). Otherwise throw, which gives `Rejected_UnexpectedEntry`. With the rules above this cannot fail, which is the point of a second check.

Size and zip-bomb rules:

- **Free-space preflight.** The sum of the declared `Length` of the extracted entries, plus `FreeSpaceSafetyMarginBytes` (64 MiB), must be at most `DriveInfo(stagingVolume).AvailableFreeSpace`. Otherwise reject with `Rejected_InsufficientSpace {0}=size`. Add the index size again if the index needs a cross-volume copy (D5).
- **Do not trust declared sizes.** Copy every entry through a bounded loop that throws `InvalidDataException` as soon as the bytes written exceed the entry's declared `Length` (or the per-entry cap above). A lying header therefore cannot fill the disk.
- No compression-ratio check. Sparse SQLite files legitimately compress more than 100:1, so a ratio check would give false positives. The bounded copy plus the free-space check cover the denial-of-service risk.
- Encrypted, unsupported or CRC-failing entries throw `InvalidDataException` and give `Rejected_Corrupted {0}=ex.Message`.
- Our code only ever writes regular files with `FileMode.CreateNew`. There is no symlink or junction creation, whatever the entry's external attributes say.
- Each extracted file is closed with `FileStream.Flush(flushToDisk: true)`. After the rename (D6), a power loss must not leave a correctly named file with unflushed, zero-filled content.

### D4: Staging location and lifetime

```
{root}\temporary\restore-{guid:N}\
    content\
        app.config
        database\app.db
        search-index\<indexName>\...
```

- Use a GUID, as IPX-0009 D5 does. The staging folder is created only after D2 passes.
- **Orphan sweep:** directly before creating the staging folder (after validation, so AC1 holds), best-effort delete `{root}\temporary\restore-*` and `{luceneParent}\*.restore-new-*`. Failures are logged and ignored. **Never** sweep `*.restore-old-*`: after a crash mid-commit those may be the only copy of the user's previous data (R1).
- `finally`: best-effort recursive delete of the staging folder and of the cross-volume index sibling, if one was created. This runs on success, on every failure and on cancellation.

### D5: Same-volume invariant: every operation after the commit point is a same-volume rename

- `content\app.config` and `content\database\app.db` are under `{root}\temporary\`, which is under the same root as their targets, so they are on the same volume by construction.
- **The search index is the exception** (R1 split): its target is under the install folder. In the `PreparingSwap` step, when `Path.GetPathRoot` of the staged index differs from that of the live Lucene directory (case-insensitive), copy `content\search-index\` to the sibling `{luceneDir}.restore-new-{guid}` and use that sibling as the rename source. This is still pre-commit and cancellable.
- Before the commit, assert that every planned rename has equal path roots. A violation throws `IOException` pre-commit, which gives `RolledBack` with nothing touched. This matters because `File.Move` across volumes **silently degrades to copy-and-delete**, which is not atomic. `Directory.Move` throws instead.
- Junctions or mount points that defeat the `GetPathRoot` check surface as an `IOException` from `Directory.Move`, or a slow copy from `File.Move`, and are handled by rollback. This is an accepted edge (R5).

### D6: The swap mechanism, per artifact: two renames each, journaled, reversible

**Chosen: rename-based for all three artifacts.** The live artifact is renamed to a sibling `*.restore-old-{guid}` (same directory, so the same volume and an atomic metadata operation), and then the staged artifact is renamed to the live path.

| Artifact | Live path | Moves (in commit order) |
|---|---|---|
| Database | `{root}\databases\app.db` plus any existing `app.db-wal`, `app.db-shm`, `app.db-journal` | 1. each existing sidecar → `{name}.restore-old-{guid}`; 2. `app.db` → `app.db.restore-old-{guid}`; 3. `content\database\app.db` → `app.db` (when the archive has a DB). `File.Move(..., overwrite: false)`. |
| Search index | `{installRoot}\databases\lucene\` (the whole directory) | 4. `lucene` → `lucene.restore-old-{guid}`; 5. staged `search-index` (or its D5 sibling) → `lucene` (when the archive has an index). `Directory.Move`. |
| Configuration | `{root}\app.config` | 6. `app.config` → `app.config.restore-old-{guid}`; 7. `content\app.config` → `app.config` (when the archive has config). `File.Move(..., overwrite: false)`. |

Why the sidecars move with the DB: if a stale `app.db-wal` from the old DB stayed next to a restored `app.db`, SQLite would replay it into the new file on the next open and **corrupt it**. Moving the old set aside together also keeps the old set consistent for rollback. The restored DB never has sidecars, because IPX-0009 normalizes it to `journal_mode=DELETE`.

Why this order: the DB is first because it is the artifact most likely to be locked (an external SQLite tool, antivirus, a second app instance). If its first rename fails, **nothing has moved yet**. The config is last because it is never externally locked, and it is followed immediately by `Reload()`.

**Alternatives considered:**

| Option | Verdict |
|---|---|
| **Copy-then-delete** (copy staged over live) | Rejected for all three. Each file is overwritten in place, so a failure mid-copy leaves a **torn live file** that cannot be repaired without a second full copy. It also needs 2x disk space and is slow for a large index. It violates AC3. |
| `File.Replace(staged, live, aside)` for the two files | Rejected. It needs the destination to exist (a fresh install has no `app.db`), its Win32 `ReplaceFile` has documented partial-failure states (`ERROR_UNABLE_TO_MOVE_REPLACEMENT_2`), and it does not apply to the directory. Two plain renames are simpler and give one uniform journal. |
| **Single top-level directory swap** | Impossible without a storage-layout change. `app.config`'s location is fixed by the package at the data root, next to `backups\` (which may contain the very zip being restored) and `temporary\`, and the index lives under a different root (R1). Introducing a `{root}\data\` folder would be a storage-location change for its own ticket (ADR territory). |
| Transactional NTFS (`MoveFileTransacted`) | Rejected. Microsoft deprecates it, it is not exposed by .NET, and it is not supported on all volumes. |

### D7: The commit point and rollback: what "no partial swap" means here

```
pre-commit (cancellable): ReadingBackup -> ExtractingBackup -> VerifyingBackup -> PreparingSwap
    ct.ThrowIfCancellationRequested();         <- last observation of the token (test hook before it)
COMMIT (token never passed, never observed):
    execute moves 1..7 in order, appending each completed move to a journal
    _appConfigurationManager.Reload()           <- on the UI thread, still rollback-able
    committed = true
post-commit (best effort, never changes the outcome):
    delete every *.restore-old-{guid}; publish Success; return Completed
```

- **Rollback = undo the journal in reverse.** Each journal entry is a rename `From → To`, and undoing it is the rename `To → From`. Undoing "staged → live" moves the new artifact back into the staging folder, which the `finally` deletes. Rollback uses only renames: no deletes, no copies, no new disk space. So it does not fail for the reasons the forward pass might (disk full, a large copy).
- A rollback step that throws is logged and **skipped**, and the remaining steps still run. Every path left behind is collected. Outcome: `RollbackIncomplete`, with exactly one Failure entry that names the leftover `*.restore-old-*` paths.
- `Reload()` throws only on an IO error reading the restored file. That triggers rollback. The old in-memory dictionary is untouched, because the assignment never happened, so disk and memory both stay at the old state. (Same placement as the sample.)
- **Commit-phase catch.** Around the commit block, catch **every** exception, unfiltered, with a comment explaining why. IPX-0008 rule 2 forbids an unfiltered catch because it would swallow cancellation, but no token is passed here, so any exception, even an unexpected `OperationCanceledException`, must lead to rollback, never to propagation. The pre-commit phase keeps the IPX-0008 filter `when (ex is not OperationCanceledException)`.
- After `committed = true`, exceptions are caught in their own guards (aside deletion: one Warning per path that cannot be deleted; Success publish: log only). The result is **always** `Completed`. This mirrors IPX-0009's `finalPath is not null` guard.

**Why this is "atomic enough" (the defence AC3 needs):**

1. **AC3's named failure, "staging fails partway (e.g. disk error)", is entirely pre-commit.** Every byte is written (extraction, the cross-volume copy) before the first live rename. A failure there leaves the live state **untouched, not restored**: there is nothing to roll back. This is a structural guarantee.
2. **In-commit failures inside the process are fully reversed.** Same-volume renames are metadata-only and allocate no data clusters, so a full disk cannot interrupt them. The realistic in-commit failure is a sharing violation (a file held without `FILE_SHARE_DELETE`). The DB-first order makes the most likely one happen before anything moves, and the reverse renames succeed for the same reason the forward ones did.
3. **The residual window is a process kill or power loss during at most 7 metadata operations** (milliseconds). Even then **no data is lost**: NTFS journals each rename (each one is applied or not, never torn), and both the old (`*.restore-old-*`) and new (`temporary\restore-*`) copies stay on disk under predictable names, which the sweep never removes (D4). The commit plan is logged at Information before execution, which gives a manual recovery path. Automatic crash recovery at startup (a persisted journal plus a bootstrap step) was considered and **rejected for this ticket**: it adds a new startup step and its tests, to cover a millisecond window that already loses no data (R1). This acceptance was **approved by the user (2026-11-06)**; startup recovery is a documented follow-up, not part of this ticket.

### D8: Live handles: release them before the swap, rely on relaunch only for in-memory state

`PreparingSwap` (pre-commit, inside `Task.Run`):

- `_indexProvider.CloseAll()`. This commits, disposes and releases `write.lock`, and is required: `Directory.Move` fails if any file in the tree is open. It is safe because the TaskRunner window is modal (IPX-0009 D8). Indexes reopen lazily, at the new directory.
- `SqliteConnection.ClearAllPools()`. This is a defensive one-liner. With `Pooling=false` there is no idle handle today, but the swap must not start to fail if pooling is ever turned on.
- `AppDbContext` is **not** closed. No connection is open (no entities, nothing queries). If a future feature holds a long-lived connection, the DB rename fails with a sharing violation and the restore rolls back cleanly, so it fails safe. That future ticket must close its connection here (R4).

After the commit, the running process is **not** torn down. Any new DB connection and any lazily reopened index already hit the restored files, and `Reload()` brings the settings dictionary in line. What stays stale until relaunch is only what was applied from the old values in memory: the theme, the UI culture, and the values the page shows. Hence the prompt (D10).

### D9: Configuration: no flush before the restore, `Reload()` right after the swap

- The VM awaits `_pendingSave` after the confirmation and before the run (IPX-0010 hand-off (b)), so no chained save can be writing `app.config.tmp` during the swap. The modal window prevents new edits.
- The handler does **not** call `SaveChangesAsync`, because the file is about to be replaced.
- `Reload()` after the swap (D7) is what stops the exit-time `SaveChangesAsync` in `App.OnCurrentHostStop` from clobbering the restored file. When the backup has no config (D11), `Reload()` yields `{}`, and the next save writes `{}`, which IPX-0006 treats as all defaults (the happy path).
- Config validation (`VerifyingBackup`) only requires that the file parses as a **JSON object** (`JsonDocument`, root `ValueKind.Object`), because `AppConfigurationManager` deserializes it to `Dictionary<string, JsonElement>`. Semantically invalid values (an unknown theme, for example) are **not** rejected: IPX-0006 recovery resets them on the next page visit.

### D10: The relaunch prompt is an information dialog, with no shutdown or relaunch code

After `RestoreOutcome.Completed`, the VM calls:

```csharp
_messageDialog.ShowInformation(
    message: T["AppConfigurationPage_Restore_Relaunch_Message"],
    title: T["AppConfigurationPage_Restore_Relaunch_Title"]);
```

- This is D14 taken literally: "prompt the user to relaunch", with no `Process.Start` (auto-relaunch is out of scope).
- **A "Close InfoPhoenix now?" Yes/No dialog was considered and rejected** for this ticket. `Application.Current.Shutdown()` raises `MainWindow.Closing`. When `ConfirmBeforeExit` is on, `ClosingHandler` would then ask "exit?" a second time, its Cancel would be **ignored** during `Shutdown` (WPF semantics), and its "stop asking" branch would start an async config save while the host is stopping. Doing this properly needs a "close already confirmed" path into `MainWindow`, which is scope this ticket does not need, because continuing to run is safe (D8). This is a small follow-up if the product owner wants it.
- This deviates from the wording `ShowQuestion` in IPX-0008 §3.3 (section 9). The rule "only `Completed` leads to the prompt" is unchanged.

### D11: The restore mirrors the backup: an artifact absent from the backup is removed from the live state

When the manifest says an artifact was absent at backup time (`contents.x == false`), its live counterpart is still moved aside (and deleted after the commit). It is **not** kept. Before the swap, the handler publishes one Warning per artifact that will be removed (`DatabaseAbsent_Warning`, `SearchIndexAbsent_Warning`, `ConfigurationAbsent_Warning`), and only when a live counterpart exists.

- Why: the index is derived from the DB. Restoring a DB while keeping a newer index (or the reverse) produces a state that never existed: search hits for documents that are not in the DB. The confirmation the user accepted in IPX-0010 says the restore "will replace all current application data (database, search index and settings) with the contents of this backup", and mirroring is what that sentence promises.
- *Alternative (the sample's):* swap only what the archive contains. It is less destructive for an "empty" backup, but it yields mixed DB/index states. Rejected.
- Effect today: backups contain config only, and installs have no DB or index, so the behaviour is identical either way. **Approved by the user (2026-11-06)** as designed, so the semantics are settled before the first DB feature lands.

## 3. Contract

### 3.1 Feature slice: `Features/PerformApplicationRestore/`

| Type | Shape |
|---|---|
| `PerformApplicationRestoreRequest` | `public sealed record PerformApplicationRestoreRequest(string SourceZipFilePath) : IRequest<RestoreOutcome>;` (IPX-0008 §3.1) |
| `RestoreOutcome` | `public enum RestoreOutcome { Completed, Blocked, RolledBack, RollbackIncomplete }`. `Blocked` = rejected by a check (D1-D3, free space, verification), and **no live file was touched**. `RolledBack` = an IO or unexpected failure in staging or commit; the live state is as before (untouched if pre-commit, reversed if in-commit). `RollbackIncomplete` = in-commit failure **and** at least one reverse rename failed; the leftovers are named in the Failure entry. This value is new (IPX-0008 allows refinements). |
| `PerformApplicationRestoreRequestHandler` | `IRequestHandler<PerformApplicationRestoreRequest, RestoreOutcome>`. Dependencies, all already registered: `IApplicationContext`, `IAppConfigurationManager`, `IIndexProvider`, `IFileManager` (DI, for Lucene), `IOptions<LuceneOptions>`, `AppDbContext` (only for `Database.GetMigrations()`, as the sample injects it), `IEventPublisher`, `ITranslator`, `ILogger<...>`. **No DI changes.** |
| `BackupCompatibility` (internal static) and `BackupRejection` (internal record `(string ReasonKey, object[] Args)`) | D1. These are pure and unit-tested directly. |
| `BackupRejectedException` (internal sealed, carries a `BackupRejection`) | Thrown by the validation helpers and caught once. It gives exactly one Failure entry `{Prefix}_Rejected_{ReasonKey}` and `Blocked`. It keeps the single-catch structure from IPX-0009 §3.2. |
| `Data/MigrationHistoryReader` (internal static) | Holds `ReadLatestMigrationId(SqliteConnection)`, **moved** out of `PerformApplicationBackupRequestHandler` (where it is currently private static) and used by both handlers. Pure refactor, no behaviour change. |

`BackupManifest` / `BackupManifestContents` stay in `Features/PerformApplicationBackup/`, and restore references them from there (IPX-0009 D3 left the choice to this ticket; no move needed).

### 3.2 Handler flow

`Prefix = nameof(PerformApplicationRestoreRequestHandler)`. Each step publishes one Information entry `{Prefix}_{Step}` from the awaiting flow, then does its work in `await Task.Run(..., ct)`. Do not use `ConfigureAwait(false)` or `SkipContextSync()` anywhere (IPX-0008 rule 5).

| # | Step | Work | Rejection or failure |
|---|---|---|---|
| 1 | `ReadingBackup` | D2 (path guard, open, entry validation, manifest, contents consistency, compatibility). Version warning (D1.4). **No writes.** | `Rejected_*`, which gives `Blocked`. An `IOException` or `UnauthorizedAccessException` opening the file gives `ReadingBackup_Failure` and `RolledBack`, with nothing touched. |
| 2 | `ExtractingBackup` | Sweep orphans (D4). Create the staging folder. Free-space preflight. Bounded, flushed, allow-listed extraction (D3). Close the archive. | `Rejected_InsufficientSpace` or `Rejected_Corrupted` (any `InvalidDataException`) give `Blocked`. IO errors give `ExtractingBackup_Failure` and `RolledBack`. |
| 3 | `VerifyingBackup` | Config is a JSON object (D9). DB: a `Mode=ReadOnly, Pooling=false` connection, then `PRAGMA trusted_schema=OFF;`, then `PRAGMA quick_check;` must return `ok`, then `MigrationHistoryReader.ReadLatestMigrationId` must **equal** `manifest.databaseMigrationId` (the manifest is untrusted; the DB itself is the truth). Dispose the connection before step 4. The index is not validated (R3). | `Rejected_ConfigurationInvalid`, `Rejected_DatabaseInvalid {0}`, or `Rejected_ContentsMismatch` (marker disagrees with the DB) give `Blocked`. A `SqliteException` gives `Rejected_DatabaseInvalid`. |
| 4 | `PreparingSwap` | D11 Warnings. D8 (`CloseAll`, `ClearAllPools`). Create the `databases\` and Lucene parent directories if missing. D5 cross-volume sibling copy. Build the move plan (D6) and assert the same-volume invariant. Log the plan at Information. Test hook `OnBeforeCommitPointCheck`, then `ct.ThrowIfCancellationRequested()`. | IO, `UnauthorizedAccessException` or `AggregateException` (from `CloseAll`) give `PreparingSwap_Failure` and `RolledBack`, with nothing touched. |
| 5 | `Swapping` | **Commit** (D7): `await Task.Run(() => ExecuteMoves(plan, journal))` with no token, then `Reload()` on the UI thread, then `committed = true`. Test hooks `OnBeforeMove` / `OnBeforeRollbackMove` (`internal Action<PlannedMove>?`, null in production). | Any exception triggers reverse rollback. That gives `Swapping_Failure {0}=ex.Message` and `RolledBack`, or `RollbackIncomplete_Failure {0}=ex.Message {1}=leftover paths` and `RollbackIncomplete`. |
| 6 | `Success` | Post-commit: delete the aside items (one `CleanupIncomplete_Warning {0}=path` per failure), publish **one** Success, return `Completed`. | Never fails the outcome. |

Structure (one catch per phase, so "exactly one Failure" is structural, as F2 requires):

```
var step = ...; var committed = false; var journal = new List<PlannedMove>();
try {
    steps 1-4
    try { step 5 commit }                                  // unfiltered catch: rollback, then one Failure, return RolledBack/RollbackIncomplete
    catch (Exception ex) { ... }
    post-commit (guarded) ; return Completed
}
catch (BackupRejectedException r) { log; one Failure Rejected_*; return Blocked; }
catch (InvalidDataException ex) when (!committed) { log; one Failure Rejected_Corrupted; return Blocked; }
catch (Exception ex) when (!committed && ex is not OperationCanceledException) { log; one Failure {step}_Failure; return RolledBack; }
finally { best-effort delete staging folder (+ cross-volume sibling) }
```

- **Cancellation:** before the commit-point check, the `OperationCanceledException` propagates after the `finally` cleanup, with no Failure entry (IPX-0008 rule 4). TaskRunner then reports `Cancelled`. After the check, the token is never observed, and a Cancel click during or after the swap has no effect on the outcome.
- Stack traces go to Serilog only. Failure entries carry `ex.Message` (IPX-0008 §5).

### 3.3 Constants (`Constants.BackupRecovery`, additive)

`RestoreStagingDirectoryPrefix = "restore-"`, `RestoreOldSuffix = ".restore-old-"`, `RestoreNewSuffix = ".restore-new-"`, `MaxArchiveEntryCount = 10_000`, `MaxManifestBytes = 64 * 1024`, `MaxConfigurationBytes = 1024 * 1024`, `FreeSpaceSafetyMarginBytes = 64L * 1024 * 1024`, `DatabaseSidecarSuffixes = ["-wal", "-shm", "-journal"]` (a `static readonly` array, since an array cannot be `const`).

### 3.4 ViewModel: `AppConfigurationPageViewModel.PerformApplicationRestoreAsync`

Replace the two IPX-0010 D5 statements (inside the existing `try`) with:

```csharp
// IPX-0010 hand-off (b): drain the save chain before app.config is replaced (never throws).
await _pendingSave;

RestoreOutcome? restoreOutcome = null;

await _taskRunner.CreateBuilder()
    .SetName(T["AppConfigurationPage_Restore_TaskRunnerWindow_Title"])
    .SubscribeFor<BackupRecoveryProgressNotificationEvent>()
    .SetDelegate(async ct => restoreOutcome = await _mediator.ExecuteAsync(
        new PerformApplicationRestoreRequest(backupFilePath), ct))
    .RunAsync();

// Key on the handler's result alone: it is the truth about disk. Completed => the swap committed,
// and the user must always be told to relaunch (D10). Every other outcome is already shown in the window.
if (restoreOutcome is RestoreOutcome.Completed) {
    _messageDialog.ShowInformation(
        message: T["AppConfigurationPage_Restore_Relaunch_Message"],
        title: T["AppConfigurationPage_Restore_Relaunch_Title"]);
}
```

- The existing `catch (Exception ex)` stays as the unexpected-bug path, with the same key `AppConfigurationPage_Restore_Failure_Message`, reworded (3.5).
- Update the XML doc comment on the command, which still says "IPX-0011 plugs the actual restore in".
- Always `await RunAsync()`, one new builder per click, and no busy flag (IPX-0008 §3.3).

### 3.5 Localization keys (both `Localization.resx` and `Localization.pt-BR.resx`; the text is a suggestion to settle in PR review)

**Remove:** `AppConfigurationPage_Restore_NotYetAvailable_Message` from both files and from `LocalizationFitnessTests.Keys` (IPX-0010 hand-off (a)).

**Reword:** `AppConfigurationPage_Restore_Failure_Message`. en: "The restore failed unexpectedly: {0}". pt-BR: "A restauração falhou inesperadamente: {0}". It now covers both the selection and the run bug paths.

**Add (VM):**

| Key | en | pt-BR |
|---|---|---|
| `AppConfigurationPage_Restore_TaskRunnerWindow_Title` | Restore application data | Restaurar os dados da aplicação |
| `AppConfigurationPage_Restore_Relaunch_Title` | Restore complete | Restauração concluída |
| `AppConfigurationPage_Restore_Relaunch_Message` | Your backup was restored.<br><br>Close and reopen InfoPhoenix to start using the restored data. Until you do, some settings and information on screen may be out of date. | Seu backup foi restaurado.<br><br>Feche e abra novamente o InfoPhoenix para começar a usar os dados restaurados. Até lá, algumas configurações e informações na tela podem estar desatualizadas. |

**Add (handler, prefix `PerformApplicationRestoreRequestHandler_`):**

| Suffix | en | pt-BR |
|---|---|---|
| `ReadingBackup` | Checking the backup file... | Verificando o arquivo de backup... |
| `ExtractingBackup` | Extracting the backup to a temporary folder... | Extraindo o backup para uma pasta temporária... |
| `VerifyingBackup` | Verifying the extracted data... | Verificando os dados extraídos... |
| `PreparingSwap` | Preparing to replace the current data... | Preparando a substituição dos dados atuais... |
| `Swapping` | Replacing the current data... | Substituindo os dados atuais... |
| `Success` | Restore complete. Restart InfoPhoenix to use the restored data. | Restauração concluída. Reinicie o InfoPhoenix para usar os dados restaurados. |
| `VersionDiffers_Warning` | This backup was created by InfoPhoenix {0}; you are running {1}. The data format is compatible, so the restore continues. | Este backup foi criado pelo InfoPhoenix {0}; você está usando a versão {1}. O formato dos dados é compatível, então a restauração continua. |
| `DatabaseAbsent_Warning` | This backup has no database; the current database will be removed. | Este backup não tem banco de dados; o banco de dados atual será removido. |
| `SearchIndexAbsent_Warning` | This backup has no search index; the current search index will be removed. | Este backup não tem índice de pesquisa; o índice de pesquisa atual será removido. |
| `ConfigurationAbsent_Warning` | This backup has no settings; your settings will return to their defaults. | Este backup não tem configurações; suas configurações voltarão ao padrão. |
| `CleanupIncomplete_Warning` | The previous data at {0} could not be deleted. You can delete it manually. | Não foi possível excluir os dados anteriores em {0}. Você pode excluí-los manualmente. |
| `Rejected_SelectedFileInDataFolder` | The selected file is inside InfoPhoenix's own data folders. Move it elsewhere (for example, to the backups folder) and try again. Nothing was changed. | O arquivo selecionado está dentro das pastas de dados do InfoPhoenix. Mova-o para outro local (por exemplo, a pasta de backups) e tente novamente. Nada foi alterado. |
| `Rejected_NotAnArchive` | The selected file is not a valid backup file. Nothing was changed. | O arquivo selecionado não é um arquivo de backup válido. Nada foi alterado. |
| `Rejected_ManifestMissing` | The selected file is not an InfoPhoenix backup. Nothing was changed. | O arquivo selecionado não é um backup do InfoPhoenix. Nada foi alterado. |
| `Rejected_ManifestInvalid` | The backup information in the selected file is unreadable or incomplete. Nothing was changed. | As informações do backup no arquivo selecionado estão ilegíveis ou incompletas. Nada foi alterado. |
| `Rejected_WrongApplication` | The selected file is a backup of another application ({0}). Nothing was changed. | O arquivo selecionado é um backup de outra aplicação ({0}). Nada foi alterado. |
| `Rejected_UnsupportedFormat` | This backup uses format version {0}, which this version of InfoPhoenix cannot read. Nothing was changed. | Este backup usa a versão de formato {0}, que esta versão do InfoPhoenix não consegue ler. Nada foi alterado. |
| `Rejected_SchemaMismatch` | This backup's database version ({0}) does not match this version of InfoPhoenix ({1}). Restoring it is not supported. Nothing was changed. | A versão do banco de dados deste backup ({0}) não corresponde a esta versão do InfoPhoenix ({1}). Restaurá-lo não é suportado. Nada foi alterado. |
| `Rejected_UnexpectedEntry` | The backup contains an unexpected or unsafe item: {0}. Nothing was changed. | O backup contém um item inesperado ou inseguro: {0}. Nada foi alterado. |
| `Rejected_ContentsMismatch` | The backup's contents do not match its description; the file may be damaged. Nothing was changed. | O conteúdo do backup não corresponde à sua descrição; o arquivo pode estar danificado. Nada foi alterado. |
| `Rejected_InsufficientSpace` | There is not enough free disk space to restore this backup ({0} needed). Nothing was changed. | Não há espaço livre suficiente em disco para restaurar este backup ({0} necessários). Nada foi alterado. |
| `Rejected_Corrupted` | The backup file is damaged: {0}. Nothing was changed. | O arquivo de backup está danificado: {0}. Nada foi alterado. |
| `Rejected_ConfigurationInvalid` | The settings in the backup are not valid. Nothing was changed. | As configurações do backup não são válidas. Nada foi alterado. |
| `Rejected_DatabaseInvalid` | The database in the backup failed its integrity check: {0}. Nothing was changed. | O banco de dados do backup falhou na verificação de integridade: {0}. Nada foi alterado. |
| `ReadingBackup_Failure` | Could not read the backup file: {0}. Nothing was changed. | Não foi possível ler o arquivo de backup: {0}. Nada foi alterado. |
| `ExtractingBackup_Failure` | Could not extract the backup: {0}. Nothing was changed. | Não foi possível extrair o backup: {0}. Nada foi alterado. |
| `VerifyingBackup_Failure` | Could not verify the backup: {0}. Nothing was changed. | Não foi possível verificar o backup: {0}. Nada foi alterado. |
| `PreparingSwap_Failure` | Could not prepare the restore: {0}. Nothing was changed. | Não foi possível preparar a restauração: {0}. Nada foi alterado. |
| `Swapping_Failure` | Could not replace the current data: {0}. Your previous data was put back unchanged. | Não foi possível substituir os dados atuais: {0}. Seus dados anteriores foram restabelecidos sem alterações. |
| `RollbackIncomplete_Failure` | The restore failed ({0}) and some of your previous data could not be put back automatically. It is kept at: {1}. Close InfoPhoenix and move these items back before using it again; the log has the details. | A restauração falhou ({0}) e parte dos seus dados anteriores não pôde ser restabelecida automaticamente. Eles estão guardados em: {1}. Feche o InfoPhoenix e mova esses itens de volta antes de usá-lo novamente; o log tem os detalhes. |

`<br><br>` means a blank line: use real line breaks in the `<value>` (the files use `xml:space="preserve"`). Every pt-BR value differs from its en value (`LocalizationFitnessTests`). For `{0}` and `{1}` in `Rejected_SchemaMismatch`, render a `null` migration ID as `-`. That avoids two extra keys for a localized "none".

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function |
|---|---|---|
| **Consistency / atomicity (AC2, AC3)** | Every write happens pre-commit. The commit is renames only, same volume (asserted), DB first, journaled, and reversed on failure. `Reload()` sits inside the commit. Sidecars move with the DB. | **F4 `DataRootSnapshot`** (a test helper): captures relative path, size, SHA-256 and `LastWriteTimeUtc` for **every** file under the data root and the Lucene root. Every `Blocked` / `RolledBack` / cancel test asserts that the before and after snapshots are equal (after excluding the cleaned-up `temporary\`). This is the literal form of "exactly as they were". |
| **Security (untrusted archive)** | Allow-list, name rules, case-insensitive duplicates, entry cap, bounded copy, free-space preflight, `trusted_schema=OFF`, manifest re-checked against the DB, no symlinks. | A theory over malicious entry names (`../x`, `..\x`, `/abs`, `C:x`, `app.config:ads`, `APP.CONFIG` duplicate, `search-index/CON`, `app.config.`, `database/app.db/..`, `other.txt`): each gives `Blocked`, one Failure, an unchanged snapshot, and **no file outside staging**. A lying-size entry gives `Blocked`. |
| **Contract integrity with IPX-0009** | One allow-list mirrors the backup's constants, and both handlers share the migration-ID reader. | **F3 round-trip:** run the real `PerformApplicationBackupRequestHandler` on seeded fixtures (config, a DB with `__EFMigrationsHistory`, a two-file index), mutate the live state, run restore on the produced zip, and assert `Completed` plus byte-equal artifacts. This catches any future drift in entry names or the manifest on either side. |
| **Operability / feedback** | One Information entry per step, exactly one Failure or one Success, "Nothing was changed" in every pre-commit message, recovery paths named in `RollbackIncomplete`. | **F1:** add the 30 handler keys and 3 VM keys to `LocalizationFitnessTests.Keys` (remove `NotYetAvailable`). **F2:** every failure test asserts exactly one `NotificationType.Failure` `BackupRecoveryProgressNotificationEvent` and zero Success entries. |
| **Evolvability (D1)** | The decided schema-only rule is one pure function, so a future marker (for example an index format) is added in one place. | Direct unit tests of `BackupCompatibility.Evaluate` (section 5, T2), including "different app version alone is never a rejection". |

## 5. Test strategy (minimum that lands with this ticket; IPX-0012 widens it)

Location: `tests/.../Features/PerformApplicationRestore/PerformApplicationRestoreRequestHandlerTests.cs` plus `BackupCompatibilityTests.cs`. Setup matches the IPX-0009 tests: a real `PhysicalFileManager` over a per-test temp root, a **separate** second temp root for the DI `IFileManager` (this exercises the R1 split and the D5 code path when the test can place it on another drive, otherwise the same-volume path), `FakeEventPublisher`, `FakeTranslator`, `FakeAppConfigurationManager`, `FakeIndexProvider`, and `AppDbContext` over `Data Source=:memory:`. Archives are built in-test with `ZipArchive` (for malicious cases) or by the real backup handler (F3).

Test infra changes: `FakeAppConfigurationManager.Reload()` currently **throws `NotSupportedException`**. Make it record `"Reload"` in `CallLog` and honour an optional `ThrowOnReload`. `FakeIndexProvider` gets an optional `ThrowOnCloseAll` if it does not already have one. Add a `DataRootSnapshot` helper (F4).

| AC | Case | Expect |
|---|---|---|
| AC1 | T1: theory over `formatVersion: 2` (`Rejected_UnsupportedFormat`), `application: "Other"` (`Rejected_WrongApplication`), a schema mismatch (`databaseMigrationId: "20990101000000_Future"` with `contents.database: true` and a `database/app.db` entry present, so the contents-consistency check passes and the schema check is what fires, while the app has no migrations: `Rejected_SchemaMismatch`), and a manifest missing `contents` (`Rejected_ManifestInvalid`). | `Blocked`. One Failure with the right `Rejected_*` key. Snapshot unchanged, **and no `temporary\restore-*` was ever created** (assert that `temporary\` does not exist when it did not before). `CloseAll` was not called. `CallLog` empty. |
| AC1 | Not a zip (random bytes), a zip without a manifest, invalid JSON, a manifest over 64 KiB. | `Blocked`, `Rejected_NotAnArchive` / `_ManifestMissing` / `_ManifestInvalid`, snapshot unchanged. |
| AC1 | The malicious-name theory (section 4), duplicates, more than 10,000 entries, contents-flag disagreement in both directions. | `Blocked`, snapshot unchanged, no file outside the temp root. |
| AC1 | Extracted DB with a bad `quick_check` (garbage bytes as `database/app.db`), or whose `__EFMigrationsHistory` disagrees with the manifest, or config that is a JSON array. | `Blocked`, staging deleted, snapshot unchanged. |
| AC1 / D1 | T2 (`BackupCompatibilityTests`): `null`/`null` with `contents.database: true` → compatible; equal non-null IDs → compatible; `contents.database: false` with any `databaseMigrationId` (including a mismatching one) → compatible (skip); mismatching IDs with a DB → `SchemaMismatch` with both IDs as args (`null` → `-`); `formatVersion: 2` → `UnsupportedFormat`; `application: "Other"` → `WrongApplication`; format and app both wrong → `UnsupportedFormat` (order); `Evaluate` has no version parameter, so app version cannot block. Plus handler tests: a different `appVersion` (`v0.9.0` vs `v1.0.0+abc`) with matching schema gives exactly one `VersionDiffers_Warning` and still `Completed`; an unparseable `appVersion` gives one Warning and `Completed`; the same version with different build metadata gives **no** warning. | As stated. |
| AC2 | All three artifacts in the archive, all three live and different. | `Completed`. Live bytes equal the archive's. No `*.restore-old-*` remains. Staging deleted. `CallLog` contains `Reload`. `CloseAll` called. Information entries in step order, then exactly one Success. |
| AC2 | Live `app.db-wal` / `-shm` exist before the restore. | After `Completed` no sidecar sits next to the restored `app.db`, and the restored DB opens and shows the backup's seeded row (the stale WAL was not replayed). |
| AC2 / D11 | The backup has config only, while a live DB and index exist. | `Completed`. Two `*Absent_Warning` entries. The live DB and index are gone. Config restored. |
| AC2 | Fresh install (no live artifacts), backup with all three. | `Completed`, artifacts created, `databases\` created. |
| AC2 | F3 round-trip. | Section 4. |
| AC3 | Disk error while staging: make the staging target unwritable (a **file** named `temporary` at the root, or a pre-created read-only file colliding with the extraction path), or have the test hook throw `IOException` mid-extraction. | `RolledBack`. One `ExtractingBackup_Failure`. Snapshot unchanged. No staging left. |
| AC3 | Free-space preflight fails (a declared entry `Length` larger than the free space, built with a stored entry whose header claims it, or a stubbed free-space provider if a test seam is cheaper). | `Blocked` (`Rejected_InsufficientSpace`), snapshot unchanged. |
| AC3 | Lying size: an entry that inflates past its declared `Length`. | `Blocked` (`Rejected_Corrupted`), snapshot unchanged. |
| AC3 | **Mid-commit failure:** hold the live `app.config` open with `FileShare.Read` (no Delete share) during the run, so move 6 fails **after** the DB and index moves already happened. | `RolledBack`. One `Swapping_Failure`. Snapshot of the DB and index **equal to before** (reverse renames worked). Staging deleted. `CallLog` has **no** `Reload`. |
| AC3 | `Reload()` throws (`ThrowOnReload = new IOException()`). | `RolledBack`, snapshot unchanged. |
| AC3 | Rollback failure: `OnBeforeRollbackMove` throws for one move. | `RollbackIncomplete`. One Failure that contains the leftover path. The `*.restore-old-*` item exists on disk. |
| AC3 | `CloseAll` throws `AggregateException`. | `RolledBack` (`PreparingSwap_Failure`), snapshot unchanged. |
| IPX-0008 | Cancel before the commit point (via `OnBeforeCommitPointCheck`), and cancel during extraction. | OCE propagates. No Failure entry. Staging deleted. Snapshot unchanged. |
| IPX-0008 | Cancel requested from `OnBeforeMove` (inside the commit). | The handler returns `Completed` (it does not throw), and all moves are done. |
| D4 | Orphan sweep: pre-existing `temporary\restore-old\` gets deleted, while a pre-existing `app.db.restore-old-x` survives. | As stated. |
| D2 | The selected zip is under `temporary\`. | `Blocked` (`Rejected_SelectedFileInDataFolder`). |
| VM | Update `AppConfigurationPageViewModelRestoreTests` B6, B7 and B10: on Yes, the VM now reaches `_taskRunner.CreateBuilder()`. The fake throws `NotSupportedException`, so assert `FakeTaskRunner.CreateBuilderCount == 1` (add this counter) and that the **one** Failure snackbar is `AppConfigurationPage_Restore_Failure_Message`. The run path itself is not unit-testable (IPX-0008 §3.3). Remove every reference to `NotYetAvailable`. | As stated. |
| Manual | Restore a real backup and check the window steps and the information dialog wording in en and pt-BR. Restore a renamed `.txt` → `.zip`. Restore with the DB held open in "DB Browser for SQLite" (expect `RolledBack`, data intact). Press Cancel during extraction of a large fixture. After `Completed`, choose not to relaunch, change the theme, exit, relaunch: the restored settings are kept, except the theme just changed. | Exploratory, once. |

## 6. Work split and developer tasks (in build order)

**BACKEND-ONLY.** One developer, `backend-developer-dotnet`.

1. Constants (3.3). Localization keys (3.5): add, reword, and remove `NotYetAvailable`. F1 update. (AC1-AC3)
2. Move `ReadLatestMigrationId` to `Data/MigrationHistoryReader` and point the backup handler at it. The IPX-0009 tests must stay green. (AC1)
3. Test infra: `FakeAppConfigurationManager.Reload`, `FakeTaskRunner.CreateBuilderCount`, `FakeIndexProvider.ThrowOnCloseAll`, the `DataRootSnapshot` helper. (AC1, AC3)
4. `BackupCompatibility.Evaluate` (D1 checks 1-3, schema only, in order: format, application, schema when `contents.database`), `BackupRejection`, and the T2 unit tests, test-first. The version-warning helper (D1 check 4: `SemVersion` equality ignoring build metadata, unparseable = differs, Warning only) lands with the handler in task 5, together with its handler tests. (AC1)
5. Handler steps 1-3 (read, validate, extract, verify) with the AC1 / security tests first, TDD. (AC1, AC3)
6. Handler steps 4-6 (prepare, commit, rollback, post-commit) with the AC2 / AC3 / cancellation tests, plus F3 round-trip. (AC2, AC3)
7. VM wiring (3.4). Update the B6/B7/B10 tests. (AC2)
8. Manual pass (section 5, last row). CHANGELOG `Added`: "Restore a backup: compatibility check, atomic swap, relaunch prompt." Update the IPX-0010 plan's hand-off list only if something deviates.

## 7. Security notes (for the developer and QA)

- **Entry point:** the local user's own button. There is no authN/authZ surface. The **content** of the zip is the attack surface (D3).
- **Tampering:** a crafted manifest can claim compatibility. The DB's own `__EFMigrationsHistory` must agree (step 3), and `quick_check` must pass. The DB is opened read-only with `trusted_schema=OFF`, and `__EFMigrationsHistory` must be a **table** in `sqlite_master` (a view could run arbitrary SQL functions).
- **Path traversal / ADS / aliasing:** D3 rules plus the full-path containment check.
- **Denial of service:** entry count cap, manifest and config caps, free-space preflight, bounded copy.
- **What a valid-but-hostile backup can still do:** replace the user's data with other data. That is the feature's purpose, and the user confirmed it (IPX-0010). It is not a vulnerability.
- **Logging:** log the commit plan (local paths, acceptable per IPX-0008 §5). **Never log manifest or config contents.**
- **ACLs:** the renamed files keep the security descriptor inherited from `temporary\`, which has the same inheritance as `databases\` and the root. Nothing new is exposed.

## 8. Risks for QA to probe

- **R1 (accepted): crash or power loss mid-commit** leaves mixed state with no data lost. Recovery is manual, using the logged plan and the `*.restore-old-*` / `temporary\restore-*` names. Automatic startup recovery is a documented follow-up (approved acceptance, D7).
- **R8 (accepted with D1): non-schema incompatibility between app versions** (a Lucene format or analyzer change, a change in what a config value means) is not caught by the schema-only rule; it only produces `VersionDiffers_Warning`. QA: confirm the Warning appears and the restore completes; confirm a different app version alone never yields `Blocked`.
- **R2: index under `Program Files`** (R1 of IPX-0009) may be read-only, so an index restore or removal fails in `PreparingSwap` with `RolledBack` and nothing touched. This is moot until indexing ships. It reinforces the IPX-0009 R1 follow-up (move the index under the data root).
- **R3: the index is not content-validated.** A damaged index in an otherwise valid backup is restored as-is and surfaces at search time. The archive CRC is the only guard. Revisit when indexing ships (for example, open it through `IIndexProvider` against the staged path once R1 is fixed).
- **R4: a future long-lived DB connection** makes every restore fail with `RolledBack` (safe, but useless). Whoever adds it must release it in `PreparingSwap`.
- **R5: junctions or mount points** between the data root and `temporary\` defeat the same-volume assert. `File.Move` then copies, which is slow and non-atomic. This is an exotic setup; it is noted, not handled.
- **R6: a second running InfoPhoenix instance** holds stale in-memory config and would overwrite the restored `app.config` when it exits. There is no single-instance guard in the app today. This is out of scope; noted.
- **R7: once the first EF migration lands**, startup must apply it (`Migrate`) before any backup can be taken. Otherwise a backup records an older *applied* ID and is rejected by the same app version under D1.3.
- Failure modes worth tests: the exit-time save clobbering the restore (no `Reload`); a stale WAL replayed into the restored DB; a Cancel during the swap; an event published from inside `Task.Run`; the relaunch dialog shown on `Blocked`; `[PerformApplicationRestoreRequestHandler_...]` raw keys; a `Blocked` run that still created `temporary\`.

## 9. Deviations from and refinements of the binding contracts

| Contract | This plan | Why |
|---|---|---|
| IPX-0008 §3.1 `RestoreOutcome { Completed, Blocked, RolledBack }` | Adds `RollbackIncomplete`. `RolledBack` also covers pre-commit IO failures, where nothing was touched. | Permitted refinement. The VM rule is unchanged (only `Completed` leads to the prompt). |
| IPX-0008 §3.3 `outcome is Completed && restoreOutcome is Completed` → `ShowQuestion` | `restoreOutcome is Completed` alone → `ShowInformation` | The handler's result is the truth about disk. The only way the TaskRunner outcome differs is window teardown, and a committed restore must still prompt. Information instead of Question: D10. |
| IPX-0008 rule 2 (no unfiltered catch) | An unfiltered catch around the commit block only | No token in scope. Any exception there must lead to rollback, never to propagation (D7). |
| Sample restore handler | Allow-listed extraction, the manifest/compatibility gate, mirror semantics, sidecars, same-volume assert, OCE propagates instead of `RolledBack`, no snackbars from the handler, no auto-relaunch | Ticket and IPX-0008 rules. |

## 10. Decisions taken with the user

### OD1 (decided 2026-11-06): compatibility rule is "schema only"

**Rule (binding, specified in D1):** a backup is restorable when `formatVersion == 1`, `application == "InfoPhoenix"`, and, **only when the backup contains a DB**, its `databaseMigrationId` equals the running app's expected migration ID. A different app version alone produces one `VersionDiffers_Warning` and **never** blocks the restore.

Why: the rule tracks what actually breaks a restore (the schema), and it keeps backups usable across app updates and machine changes, which is the epic's main reason to exist.

Rejected alternatives, for the record: "strict" (same app version **and** same migration ID) would make every backup unrestorable after any update, even a patch, with no migration path in the epic. "App version only" (or a Major.Minor range) ignores the schema and invents a policy not tied to the data.

Accepted consequence (R8), and a **rule for future tickets**: a non-schema incompatibility between app versions (a Lucene format or analyzer change, a change in what a config value means) is not caught by this rule. Config self-heals via IPX-0006. Any future ticket that changes the search-index format, or anything else a restore depends on beyond the DB schema, must add its own marker in that ticket (bump `formatVersion`, or add a field such as `searchIndexFormat` to the manifest and a check to `BackupCompatibility.Evaluate`).

### D11 and D7 (approved as designed, 2026-11-06)

- D11: mirror semantics. An artifact absent from the backup is removed from the live state, with one Warning per removed artifact.
- D7: the crash window during the commit is accepted without automatic recovery at startup. Startup recovery is a documented follow-up (R1).

### Non-blocking product preference (default chosen, override if wanted)

- D10: an information-only relaunch prompt. Alternative: "Close now?" with Yes → close, which needs a small `MainWindow` change.

## 11. UX

**No UX spec.** The flow reuses the TaskRunner window (IPX-0008 §7) and `IMessageDialog`, and CLAUDE.md says to skip `ux-specialist`. The real UX moments are wording only:

- The relaunch dialog says what happened, what to do (close and reopen), and what to expect if the user does not (out-of-date screen).
- Every pre-commit failure ends with "Nothing was changed" so the user knows their data is safe.
- `RollbackIncomplete` tells the user exactly where their data is.

PR review settles the text.

## 12. Why no ADR

The ADR categories are structural framework change, external API integration and database mutation. This ticket is none of them. It changes no schema and no storage location, and it adds no dependency (`System.IO.Compression`, `Microsoft.Data.Sqlite` and EF are already used). It replaces data files with the user's own earlier copies through existing primitives, following the sample's pattern. The decisions are recorded in this plan, including the decided OD1 policy (section 10). If the product owner wants its "future format changes must add a marker" rule to bind future tickets more formally, a one-paragraph ADR could capture it, but this plan does not create one.

## 13. Calls most worth a second opinion

All three calls flagged in the first design pass are now settled by the user (section 10): OD1 (schema only), D11 (mirror semantics) and D7 (crash window accepted, startup recovery as a follow-up). For PR review, the remaining judgement calls are the D10 information-only prompt and the localization wording (3.5).

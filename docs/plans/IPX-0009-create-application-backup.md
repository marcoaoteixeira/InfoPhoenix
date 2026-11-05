# IPX-0009: Create a local backup ("Back up now")

- Ticket: [IPX-0009 (#25)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/25)
- Epic: [IPX-0007 Backup & Restore (#23)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23), decisions D2, D4-D11.
- Binding contract: [IPX-0008 plan](IPX-0008-progress-error-surfacing-mechanism.md) sections 1 and 3. This plan deviates from it nowhere. Where it adds detail, the detail is marked as such.
- Stack: .NET 10 / WPF (`dotnet`), one project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. There is no separate frontend.
- UX spec: **not needed**. See section 10.
- ADR: **none**. See section 11.

## 1. Context: what exists today (verified in this worktree and in the packages)

| Concern | Where it really lives | Source |
|---|---|---|
| App data root | `IApplicationContext.FileManager.Root`. `ApplicationContextOptions.ApplicationDataLocation` is not configured anywhere in InfoPhoenix, so it uses the enum default `Machine`, which is **`C:\ProgramData\InfoPhoenix\`**. The "Open application data directory" card opens this folder. | `Nameless.Common` `ApplicationContext.GetApplicationDataDirectory` |
| SQLite DB | `{root}\databases\app.db` (`FileManager.GetDatabaseDirectory()` + `Constants.Database.FileName`), connection string `Pooling=false`. | `Infrastructure/ApplicationHostFeature.cs` |
| App configuration | `{root}\app.config` (JSON). `AppConfigurationManager` writes it with write-then-rename through `app.config.tmp`. The file name is a private constant in the package. | `Nameless.Windows` `AppConfigurationManager` |
| Lucene index | `IIndexProvider` (singleton, registered by `LuceneHostFeature` inside `HostFeatureCollection.CreateDefault()`) opens `{IFileManager.Root}\{LuceneOptions.DirectoryRelativePath}\{indexName}`. `appsettings.json` sets `Lucene:DirectoryRelativePath = databases/lucene`. **The DI-registered `IFileManager` is rooted at `AppContext.BaseDirectory`, not at the app data root.** The sample fixes that with `CustomFileManagerHostFeature`, but InfoPhoenix does not have it (see risk R1). | `Nameless.Common` `PhysicalFileManagerHostFeature`, `Index.CreateFSDirectory` |
| Backup folder helper | `FileManager.GetBackupDirectory()` returns `{root}\backups`. `GetTemporaryDirectory()` returns `{root}\temporary`. | `Nameless.Windows` `FileManagerExtensions` (present in 4.0.0) |
| Index flush/unlock | `IIndexProvider.CloseAll()` commits and disposes every open index, which releases `write.lock`. Indexes reopen on the next `Get`. | `Nameless.Common` 4.0.0 |
| App version | `IApplicationContext.Version`, a normalized SemVer string with a prefix, built from `AssemblyInformationalVersion` in `App.BuildHostArgs`. It is the same string the Version card shows. | `App.xaml.cs`, `ApplicationContext.GetVersion` |
| EF migrations | **None exist.** `AppDbContext` has no entities, and nothing calls `Migrate`/`EnsureCreated`. Today, **`app.db` is never created and no Lucene index is ever written.** | `Data/AppDbContext.cs` |
| Clock | `TimeProvider` is registered (`TimeProvider.System`) by `CommonHostFeature`. | `Nameless.Common` |
| Backup card | `Views/Pages/AppConfigurationPage.xaml`: `ApplicationBackupCard` (`IsEnabled="False"`, "coming soon" tooltip), `ApplicationBackupBadge`, and `ApplicationBackupButton` with no command (IPX-0005 stub). | XAML |

The last row of the EF line matters for QA: on a fresh install, a backup currently contains only `app.config` and the manifest (section 3.4). Tests must create the DB and index fixtures themselves.

## 2. Decisions

### D1: Fixed backup folder = `IApplicationContext.FileManager.GetBackupDirectory()`, which is `C:\ProgramData\InfoPhoenix\backups\` today

There is a clear precedent, so this is not a NEEDS-DECISION. `Nameless.Windows` ships `GetBackupDirectory()` for exactly this purpose, the reference sample uses it, and it sits under the same root as the DB and `app.config`, so the user finds it through the existing "Open application data directory" button. The folder is created on demand (`Create()` is idempotent).

*Alternatives considered:*
- A hard-coded `%LOCALAPPDATA%\InfoPhoenix\Backups\`. Rejected: it invents a second root next to the one the app already uses, so backups and data would sit in different trees.
- Switching `ApplicationDataLocation` to `User`. Rejected: it moves the whole data root, which is a storage-location change outside this ticket.

Whatever the root resolves to later, backups follow it automatically.

### D2: File name `InfoPhoenix-backup-{UTC yyyyMMdd'T'HHmmss'Z'}.zip`, never overwritten

- Example: `InfoPhoenix-backup-20261105T143012Z.zip`.
- **UTC**, taken once at the start of the run from the injected `TimeProvider`. It sorts lexicographically, the DST fall-back hour cannot produce duplicate names, and the `Z` makes the zone explicit.
- The `InfoPhoenix-` prefix keeps the file recognizable after it is copied to another machine (the epic's "machine change" goal).
- **No overwrite, by construction (AC3):** the final step is `File.Move(staged, final, overwrite: false)`. If the target already exists (two runs in the same second), retry with `-2`, `-3`, ... (`InfoPhoenix-backup-20261105T143012Z-2.zip`), up to 99 attempts. If all of them exist, that is an anticipated failure. The timestamp alone is not the guarantee; the non-overwriting move is.
- Pattern constant: `Constants.BackupRecovery.FileNamePattern = "InfoPhoenix-backup-{0:yyyyMMdd'T'HHmmss'Z'}.zip"`, formatted with `CultureInfo.InvariantCulture`. The collision suffix is inserted before `.zip`.

*Alternative:* the sample's `backup-{yyyyMMddHHmmss}.zip`. It works, but it is ambiguous about the time zone, and on a moved file it does not say which app it belongs to.

### D3: Version/schema marker = `backup-manifest.json` at the archive root

```json
{
  "formatVersion": 1,
  "application": "InfoPhoenix",
  "appVersion": "v1.4.0",
  "databaseMigrationId": "20261201093000_Initial",
  "createdAtUtc": "2026-11-05T14:30:12+00:00",
  "contents": {
    "database": true,
    "searchIndex": false,
    "configuration": true
  }
}
```

| Field | Type | Source | Why |
|---|---|---|---|
| `formatVersion` | int, always `1` for now | `Constants.BackupRecovery.FormatVersion` | Describes the **archive layout**. IPX-0011 rejects unknown values. This is independent of the app and DB versions, so the layout can evolve later without guessing. |
| `application` | string | `Constants.Application.Name` | Cheap guard against restoring a zip from another app. |
| `appVersion` | string | `IApplicationContext.Version`, verbatim | Running app version. IPX-0011 compares it to the version the running app reads from the same source, so like is compared with like. |
| `databaseMigrationId` | string, nullable | The newest `MigrationId` in `__EFMigrationsHistory` **of the staged DB copy**. `null` when there is no DB, or no history table (as today). | Schema of the **data actually archived**. It is read from the copy, not the live DB, so it always matches the bytes in the zip. It never touches or creates the live file. |
| `createdAtUtc` | ISO-8601 `DateTimeOffset` | The same `TimeProvider` instant as the file name | Information only, for display in IPX-0011. |
| `contents.*` | bool | Whether each artifact was present at backup time | Restore must distinguish "artifact absent at backup time" from "archive corrupted". See D6. |

**Both** versions are recorded, as the ticket's question asks. Which ones IPX-0011 compares, and how (equality, or migration ID only), is decided there. This ticket only guarantees that the data is present and truthful.

- Serialization: `System.Text.Json`, `JsonSerializerDefaults.Web` (camelCase), indented. The record type `BackupManifest` (with nested `BackupManifestContents`) lives in the backup slice. IPX-0011 references it from there; moving it into a shared folder is IPX-0011's call if it needs to.
- Unknown fields must be ignored on read, so `formatVersion` 1 readers tolerate additive fields.

### D4: Archive layout is logical, not mirrored from disk

```
backup-manifest.json
app.config
database/app.db
search-index/<indexName>/...     (contents of {IFileManager.Root}\{LuceneOptions.DirectoryRelativePath}, minus write.lock)
```

The DB and the index resolve against **different roots** today (R1). Mirroring physical paths would bake that accident into the archive format. Logical entry names let IPX-0011 map each entry to wherever the current code resolves that artifact. Entry names are constants in `Constants.BackupRecovery` (`ManifestEntryName`, `ConfigurationEntryName`, `DatabaseEntryName`, `SearchIndexEntryDirectory`).

### D5: Stage everything in a per-run temp folder, zip inside it, then do one atomic move into `backups\`

```
{root}\temporary\backup-{guid:N}\
    content\          <- config copy, DB online-backup copy, index copy, manifest
    archive.zip       <- built from content\
```

Then `File.Move(archive.zip, {root}\backups\<name>.zip, overwrite: false)`.

- `temporary\` and `backups\` are both under the same root, so they are on the same volume and the move is an atomic rename. **`backups\` never contains a partial file.** A file there is either complete or absent.
- **Cleanup is one operation:** delete `temporary\backup-{guid}\` recursively in a `finally`. That removes the half-written zip and every staging file, which is what "no half-written zip or temp/staging file remains" (AC2, epic D10) means here.
- Use a GUID, not `GetTemporaryDirectory(timestamp)`. The timestamp variant has one-second resolution and could collide with a second run in the same second.
- **Orphan sweep (crash safety, added detail):** at the start of each run, best-effort delete any leftover `temporary\backup-*` folders. A `finally` cannot run if the process is killed, and the next backup then cleans up after the crash. Sweep failures are logged and ignored.

*Alternative considered:* write `<name>.zip.partial` directly in `backups\` and rename on success. It is equally atomic, but a crash leaves a visible partial file in the user's backup folder (and in IPX-0010's Open dialog), and cleanup needs two locations. Rejected.

### D6: A missing source artifact is a warning, not a failure

When `app.db`, the index folder or `app.config` does not exist, publish one **Warning** entry naming it, record `false` in `contents`, and continue. As section 1 explains, `app.db` and the index do not exist on any install today. Failing would make the button unusable until those features land, and an empty-but-honest backup is what the sample does too. Restore (IPX-0011) uses `contents` to know that a missing entry is expected.

This choice is within design remit. It does not change the AC, because AC1's scenario has all three artifacts present, but QA should know about it.

### D7: SQLite is copied through the online-backup API, never with a file copy

`SqliteConnection.BackupDatabase` is used from a `Mode=ReadOnly, Pooling=false` source connection to a `Pooling=false` destination at `content\database\app.db`, inside `Task.Run`. It produces a transactionally consistent copy even if an EF connection is open. A raw `File.Copy` could capture a torn page, or miss a `-wal`/`-journal`.

- **Check `File.Exists` first.** `ReadOnly` mode fails rather than creating the file, but the explicit check produces the D6 warning instead of an exception.
- **`Pooling=false` on both connections is mandatory.** A pooled connection keeps the staged file open, and then the zip step or the cleanup fails with "file in use". Dispose both connections before zipping.
- `AppDbContext` is **not** injected. No close/flush is needed: `Pooling=false` means EF holds no idle handle, and the backup API does not need exclusive access. This differs from the sample's `CloseConnectionAsync` call, which the sample itself labels "best-effort parity".
- `databaseMigrationId` (D3) is read from the **staged copy** after the backup, with its own `ReadOnly, Pooling=false` connection: first check `sqlite_master` for `__EFMigrationsHistory`, then `SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1`.

### D8: The Lucene index is flushed and unlocked before copying

1. Call `_indexProvider.CloseAll()`. It commits pending writes and releases `write.lock`. It is safe here because the TaskRunner window is modal and nothing else in InfoPhoenix uses an index today. Indexes reopen lazily on the next `Get`.
2. Recursively copy `{IFileManager.Root}\{LuceneOptions.DirectoryRelativePath}` into `content\search-index\`, **skipping `write.lock`**. Do this inside `Task.Run`.

Resolve the index directory through the **DI `IFileManager` plus `IOptions<LuceneOptions>`**, the same pair `IndexProvider` uses, and not through `IApplicationContext.FileManager`. That way the backup copies the index from wherever it really lives, including after R1 is fixed.

`CloseAll` can throw `AggregateException`. That falls under the "anticipated" catch (section 4).

### D9: Configuration is flushed, then copied as a file

1. In the ViewModel, before starting the TaskRunner, `await _pendingSave` (the IPX-0004 save chain). This guarantees that no chained save is still writing `app.config.tmp`. Added detail: the chain already swallows its own faults, so this await does not throw.
2. In the handler, `await _appConfigurationManager.SaveChangesAsync(ct)`. This flushes in-memory changes made outside the page. It is safe now because the chain is drained and the modal window blocks further edits.
3. Copy `{root}\app.config` (`IApplicationContext.FileManager.GetFile(Constants.BackupRecovery.AppConfigurationFileName)`) into `content\app.config`.

The package keeps the file name `"app.config"` private, so it is duplicated in `Constants.BackupRecovery.AppConfigurationFileName`. Guard it with a fitness test (section 6, F4).

## 3. Contract

### 3.1 Feature slice: `Features/PerformApplicationBackup/`

| Type | Shape | Notes |
|---|---|---|
| `PerformApplicationBackupRequest` | `public sealed record PerformApplicationBackupRequest : IRequest<PerformApplicationBackupResult>;` | IPX-0008 §3.1 |
| `PerformApplicationBackupResult` | `public sealed record PerformApplicationBackupResult(string? BackupFilePath) { public bool Succeeded => BackupFilePath is not null; }` | IPX-0008 §3.1. `BackupFilePath` is the **full path** of the final zip. Add `static Failed => new(null)` if convenient. |
| `PerformApplicationBackupRequestHandler` | `IRequestHandler<PerformApplicationBackupRequest, PerformApplicationBackupResult>` | Dependencies: `IApplicationContext`, `IAppConfigurationManager`, `IIndexProvider`, `IFileManager`, `IOptions<LuceneOptions>`, `TimeProvider`, `IEventPublisher`, `ITranslator`, `ILogger<PerformApplicationBackupRequestHandler>`. All are already registered. No DI changes. |
| `BackupManifest`, `BackupManifestContents` | `sealed record`s per D3 | Read by IPX-0011. |

Mediator handlers are discovered by assembly scan (as `OpenCurrentLogFileRequestHandler` is), so no registration is needed.

### 3.2 Handler flow (every step follows IPX-0008 §3.2)

Each step: publish one **Information** entry from the awaiting flow, then do the work. Synchronous IO goes in `await Task.Run(..., ct)`. Never use `ConfigureAwait(false)` or `SkipContextSync()`.

| # | Step (log entry key suffix) | Work | Failure key suffix |
|---|---|---|---|
| 0 | `Preparing` | Sweep orphan `temporary\backup-*` folders (best effort). Create `temporary\backup-{guid}\content\`. Take `timestamp = timeProvider.GetUtcNow()`. | `Preparing_Failure` |
| 1 | `CopyingConfiguration` | D9 steps 2-3. A missing file produces `ConfigurationMissing_Warning`. | `CopyingConfiguration_Failure` |
| 2 | `CopyingDatabase` | D7. A missing file produces `DatabaseMissing_Warning`. | `CopyingDatabase_Failure` |
| 3 | `CopyingSearchIndex` | D8. A missing folder produces `SearchIndexMissing_Warning`. | `CopyingSearchIndex_Failure` |
| 4 | `CreatingArchive` | Read the migration ID from the staged DB. Write `backup-manifest.json` into `content\`. `await Task.Run(() => ZipFile.CreateFromDirectoryAsync(content, archive.zip, CompressionLevel.Optimal, includeBaseDirectory: false, ct), ct)`. Wrapping the async zip in `Task.Run` keeps the Deflate CPU work off the UI thread, which a bare `await` would not. Then `ct.ThrowIfCancellationRequested()`. Then `backups\` `Create()` and the non-overwriting move with the collision suffix (D2). **The move is the commit point: do not observe `ct` after it.** | `CreatingArchive_Failure` |
| 5 | `Success` | Publish **one Success** entry naming the full path. Return `new(finalPath)`. | none |

```
try {
    steps 0-5
} catch (Exception ex) when (ex is not OperationCanceledException) {
    log error (with stack trace, to Serilog only)
    publish exactly ONE Failure: T.Get("<Prefix>_<CurrentStep>_Failure", ex.Message)
    return new(null)                          // no rethrow
} finally {
    best-effort delete temporary\backup-{guid}\ (recursive); log a warning if it fails
}
```

- Track the current step in a local variable so there is **one** catch. Per-step try/catch blocks, as in the sample, are not needed, and a single catch makes "exactly one Failure" structural (F2).
- **Cleanup in the `finally` runs on success, on failure and on cancellation.** On success the staging folder is just as disposable.
- **Cancellation:** the `OperationCanceledException` propagates (it is excluded from the filter) after the `finally` cleans up. No Failure entry is published. TaskRunner logs its own Cancelled warning (IPX-0008 §3.2.4).
- **"Unwritable target":** the `backups\` `Create()` or the `Move` throws `UnauthorizedAccessException`/`IOException` and lands in the single catch during the `CreatingArchive` step. Nothing reached `backups\`.
- **Disk full:** `IOException` during copy or zip, then the catch. The staging folder (including the partial `archive.zip`) is deleted in the `finally`.
- **Collision exhaustion** (99 names taken): throw `IOException` inside the step, so it is reported as a `CreatingArchive_Failure`.
- `Prefix` = `nameof(PerformApplicationBackupRequestHandler)`, matching the existing `OpenCurrentLogFileRequestHandler_*` key style.

### 3.3 ViewModel: `AppConfigurationPageViewModel`

- Add a constructor parameter `ITaskRunner taskRunner`, guarded with `Throws.When.Null`, like the others.
- Add a command that follows IPX-0008 §3.3 verbatim, with one addition at the top:

```csharp
[RelayCommand]
private async Task PerformApplicationBackupAsync() {
    await _pendingSave; // D9: drain the IPX-0004 save chain first (never faults).

    PerformApplicationBackupResult? result = null;
    try {
        var outcome = await _taskRunner.CreateBuilder()
            .SetName(T["AppConfigurationPage_Backup_TaskRunnerWindow_Title"])
            .SubscribeFor<BackupRecoveryProgressNotificationEvent>()
            .SetDelegate(async ct => result = await _mediator.ExecuteAsync(new PerformApplicationBackupRequest(), ct))
            .RunAsync();

        if (outcome is TaskRunnerOutcome.Completed && result is { Succeeded: true }) {
            await _eventPublisher.PublishSuccessAsync<SnackBarNotificationEvent>(
                message: T.Get("AppConfigurationPage_Backup_Success_Message", result.BackupFilePath));
        }
    }
    catch (Exception ex) {
        _logger.LogError(ex, "Backup failed unexpectedly.");
        await _eventPublisher.PublishFailureAsync<SnackBarNotificationEvent>(
            message: T.Get("AppConfigurationPage_Backup_Failure_Message", ex.Message));
    }
}
```

The generated command is `PerformApplicationBackupCommand`. `AsyncRelayCommand` already disables itself while running, and the modal window prevents re-entrancy, so no busy flag is needed.

### 3.4 View: `Views/Pages/AppConfigurationPage.xaml`, `ApplicationBackupCard`

- Remove `IsEnabled="False"`, `ToolTip="{loc:Translator AppConfigurationPage_ComingSoon_Tooltip}"` and `ToolTipService.ShowOnDisabled="True"` from `ApplicationBackupCard`.
- Remove the `ApplicationBackupBadge` element. Keep the `StackPanel` with the title, or collapse it to the title `TextBlock`; either is fine.
- `ApplicationBackupButton`: add `Cursor="Hand"` and `Command="{Binding ElementName=AppConfigurationPageRoot, Path=ViewModel.PerformApplicationBackupCommand, Mode=OneTime}"`, the same binding style as the other cards.
- Do **not** bind a command on the `CardControl` itself, so the card does not run a backup on a click outside the button. That matches how the other cards work.
- Restore and Check-for-update stay stubbed. `ComingSoon_*` keys stay.

### 3.5 Localization keys: add to `Localization.resx` (en) and `Localization.pt-BR.resx`

From IPX-0008 §3.4, unchanged: `TaskRunnerWindow_CancelButton_Caption`, `TaskRunnerWindow_CloseButton_Caption`, `TaskRunnerWindow_Cancelled_Message`, `TaskRunnerWindow_CloseBlocked_Message`, with the text given there.

Owned by this ticket. The text is a suggestion, to be settled in PR review:

| Key | en | pt-BR |
|---|---|---|
| `AppConfigurationPage_Backup_TaskRunnerWindow_Title` | Back up application data | Backup dos dados da aplicação |
| `AppConfigurationPage_Backup_Success_Message` | Backup created: {0} | Backup criado: {0} |
| `AppConfigurationPage_Backup_Failure_Message` | Backup failed unexpectedly: {0} | O backup falhou inesperadamente: {0} |
| `PerformApplicationBackupRequestHandler_Preparing` | Preparing backup... | Preparando o backup... |
| `PerformApplicationBackupRequestHandler_CopyingConfiguration` | Saving and copying settings... | Salvando e copiando as configurações... |
| `PerformApplicationBackupRequestHandler_CopyingDatabase` | Copying database... | Copiando o banco de dados... |
| `PerformApplicationBackupRequestHandler_CopyingSearchIndex` | Copying search index... | Copiando o índice de pesquisa... |
| `PerformApplicationBackupRequestHandler_CreatingArchive` | Creating backup file... | Criando o arquivo de backup... |
| `PerformApplicationBackupRequestHandler_Success` | Backup created: {0} | Backup criado: {0} |
| `PerformApplicationBackupRequestHandler_ConfigurationMissing_Warning` | No settings file found; backup continues without it. | Arquivo de configurações não encontrado; o backup continua sem ele. |
| `PerformApplicationBackupRequestHandler_DatabaseMissing_Warning` | No database found; backup continues without it. | Banco de dados não encontrado; o backup continua sem ele. |
| `PerformApplicationBackupRequestHandler_SearchIndexMissing_Warning` | No search index found; backup continues without it. | Índice de pesquisa não encontrado; o backup continua sem ele. |
| `PerformApplicationBackupRequestHandler_Preparing_Failure` | Backup failed while preparing: {0} | O backup falhou na preparação: {0} |
| `PerformApplicationBackupRequestHandler_CopyingConfiguration_Failure` | Backup failed while copying settings: {0} | O backup falhou ao copiar as configurações: {0} |
| `PerformApplicationBackupRequestHandler_CopyingDatabase_Failure` | Backup failed while copying the database: {0} | O backup falhou ao copiar o banco de dados: {0} |
| `PerformApplicationBackupRequestHandler_CopyingSearchIndex_Failure` | Backup failed while copying the search index: {0} | O backup falhou ao copiar o índice de pesquisa: {0} |
| `PerformApplicationBackupRequestHandler_CreatingArchive_Failure` | Backup failed while creating the backup file: {0} | O backup falhou ao criar o arquivo de backup: {0} |

Every pt-BR value differs from en, which `LocalizationFitnessTests` requires.

### 3.6 Constants (`Constants.cs`, new nested class)

`Constants.BackupRecovery`: `FileNamePattern`, `MaxCollisionSuffix = 99`, `StagingDirectoryPrefix = "backup-"`, `AppConfigurationFileName = "app.config"`, `ManifestEntryName = "backup-manifest.json"`, `ConfigurationEntryName = "app.config"`, `DatabaseEntryName = "database/app.db"`, `SearchIndexEntryDirectory = "search-index"`, `LuceneWriteLockFileName = "write.lock"`, `FormatVersion = 1`. Zip entry names always use `/`.

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function |
|---|---|---|
| Consistency / no partial state (AC2, AC3) | Staging plus an atomic same-volume move. `backups\` only ever receives complete files. `finally` cleanup and an orphan sweep. Non-overwriting move. | Handler tests: after every failure case, `backups\` is empty and `temporary\` has no `backup-*` folder. A collision test: two runs with a frozen `TimeProvider` produce two distinct files, and the first file's bytes are unchanged. |
| Data integrity | SQLite online backup instead of a file copy. Index flushed (`CloseAll`) and `write.lock` skipped. The marker is read from the staged copy. | Test: the archived `database/app.db` opens and a seeded row is readable. The manifest `databaseMigrationId` equals the seeded history row. |
| Operability / feedback (IPX-0008) | One Information entry per step, exactly one Success or one Failure, a snackbar naming the file. | **F1** (extend `LocalizationFitnessTests.Keys` with the 4 `TaskRunnerWindow_*` keys and all 17 keys from 3.5). **F2**: every failure test asserts exactly one `NotificationType.Failure` `BackupRecoveryProgressNotificationEvent` and zero Success entries. |
| Evolvability of the archive contract (IPX-0011 depends on it) | `formatVersion`, logical entry names, a `contents` map. | **F3**: an archive-shape test asserts the exact entry set and that `backup-manifest.json` deserializes into `BackupManifest` with `FormatVersion == 1`. **F4** (in `ConstantsFitnessTests`): saving through the real `AppConfigurationManager` against a temp-root `FileManager` produces a file named `Constants.BackupRecovery.AppConfigurationFileName`. This catches a renamed private constant at the next package bump. |
| Responsiveness | Synchronous IO and zip compression are wrapped in `Task.Run`. | Manual check only (IPX-0008 §4). The reviewer should check for any synchronous `File.*`/`Directory.*` call outside a `Task.Run` body. |

## 5. Security notes

- No new entry point. It is the local user's own button, in a single-user desktop app.
- The archive contains the user's own data in clear text. Backups are not encrypted in this epic (not requested). The ZIP inherits the ACLs of `C:\ProgramData\InfoPhoenix\backups\`.
- Failure entries and the snackbar include `ex.Message`, which may contain local paths. That is acceptable (IPX-0008 §5). Stack traces go to Serilog only.
- **Never follow reparse points** while copying the index directory. Skip any directory with `FileAttributes.ReparsePoint`, so a junction inside the index folder cannot pull arbitrary files into the archive. It costs one line and costs nothing in normal use.
- Write zip entry names only from the fixed constants and relative paths under the index root. Do not reproduce absolute paths, so the archive does not leak the machine layout and IPX-0011's zip-slip check stays simple.

## 6. Test strategy

Handler tests go in `tests/Nameless.InfoPhoenix.Client.Tests/Features/PerformApplicationBackup/PerformApplicationBackupRequestHandlerTests.cs`. They use a real `PhysicalFileManager` over a per-test temp root (one for `IApplicationContext.FileManager`, and the same or a second one for the DI `IFileManager`, so the R1 split is exercised), `FakeEventPublisher`, `FakeTranslator`, `FakeAppConfigurationManager`, a hand-rolled `FakeIndexProvider` that records `CloseAll`, and a hand-rolled `TimeProvider` subclass with a fixed `GetUtcNow`. Do not add `Microsoft.Extensions.TimeProvider.Testing` for this.

| AC | Case | Level |
|---|---|---|
| AC1 | All three artifacts seeded, so the zip appears in `backups\` with a name matching the pattern. Entries per D4. Manifest fields correct. Exactly one Success entry naming the full path. Result `Succeeded`. `CloseAll` called once. | Integration (temp folder) |
| AC1 | `write.lock` present in the index, so it is absent from the archive. | Integration |
| AC1 | The archived DB is readable, and the migration ID is read from the staged copy (seed `__EFMigrationsHistory` by hand). | Integration |
| AC1 / D6 | No DB, no index, no config: success with 3 Warning entries and `contents` all `false`. The **live `app.db` was not created** by the backup. | Integration |
| AC2 | Target unwritable: create a **file** named `backups` at the root so `Create()` throws. Expect one Failure entry (`CreatingArchive_Failure`), a `null` path, no exception, and no `temporary\backup-*` left behind. | Integration |
| AC2 | Source unreadable: hold `app.db` open with `FileShare.None`, or lock an index file. Expect one Failure entry for the right step and cleanup done. | Integration |
| AC2 | `CloseAll` throws: expect `CopyingSearchIndex_Failure` and cleanup. | Unit |
| AC2 | Orphan sweep: a pre-existing `temporary\backup-old\` is deleted by the next run. | Integration |
| AC2 / IPX-0008 §3.2.4 | Cancelled before the move: `OperationCanceledException` propagates, no Failure entry, staging is deleted, `backups\` is empty. | Unit |
| AC3 | Two runs with the **same frozen timestamp** produce `...Z.zip` and `...Z-2.zip`, and the first file is byte-identical before and after the second run. | Integration |
| UI | `AppConfigurationPageTests`: **update** the IPX-0005 theories by removing `ApplicationBackupCard`, `ApplicationBackupButton` and `ApplicationBackupBadge` from the "stub" `InlineData`. Add a test that `ApplicationBackupCard` is enabled and `ApplicationBackupButton.Command` is not null. | Component (STA) |
| UI | Every place that builds `AppConfigurationPageViewModel` (3 test files) gets a `FakeTaskRunner` whose `CreateBuilder()` throws `NotSupportedException`. The run path cannot be unit-tested (IPX-0008 §3.3). | n/a |
| Manual | Click "Create backup", then watch the window steps, the Success line, Close, and the snackbar with the path. Also: a read-only `backups` folder shows the Failure line and leaves nothing behind. Cancel mid-run. pt-BR texts. | Exploratory, once |

IPX-0012 adds the wider matrix. These tests are the minimum that has to land with this ticket.

## 7. Work split and developer tasks (in build order)

**BACKEND-ONLY.** One developer, `backend-developer-dotnet`.

1. Add `Constants.BackupRecovery` (3.6) and the F4 constants fitness test. (D2, D3, D4, D9)
2. Add the 4 `TaskRunnerWindow_*` keys and the 17 IPX-0009 keys to both `.resx` files, and extend `LocalizationFitnessTests.Keys` (F1). (AC1, AC2)
3. Add `PerformApplicationBackupRequest`, `PerformApplicationBackupResult`, `BackupManifest` and `BackupManifestContents`. (AC1)
4. Implement `PerformApplicationBackupRequestHandler` per 3.2 / D5-D9, together with its tests from section 6, TDD-style, starting with the AC2 cleanup cases. (AC1, AC2, AC3)
5. Inject `ITaskRunner` into `AppConfigurationPageViewModel`, add `PerformApplicationBackupCommand` (3.3), and add `FakeTaskRunner` to the 3 test files. (AC1, AC2)
6. Enable the card and bind the button (3.4). Update the IPX-0005 stub tests and add the enabled/bound test. (AC1)
7. Do a manual pass (section 6, last row). Add a CHANGELOG `Added` entry. Add a README line saying backups go to `<app data>\backups\` if the README has a user-facing data section; otherwise leave it.

## 8. Risks and open points

- **R1: Lucene root split (pre-existing, not fixed here).** The DI `IFileManager` that `IndexProvider` uses is rooted at the install directory (`AppContext.BaseDirectory`), while the DB and config live under `C:\ProgramData\InfoPhoenix\`. Once indexing is built, the index would sit under the install folder, which is likely read-only under `Program Files`. The backup is **correct either way** (D8 resolves it through the same `IFileManager`). The fix is a separate ticket: register `IFileManager` as `IApplicationContext.FileManager`, like the sample's `CustomFileManagerHostFeature`. That fix is a storage-location change, so it may warrant an ADR. **Recommendation: open a follow-up ticket before any indexing feature lands.** It needs no decision now.
- **R2: On today's installs, a backup contains only `app.config` and the manifest** (no DB or index yet). That is expected (D6). Do not file it as a bug.
- **R3: `IAppConfigurationManager.SaveChangesAsync` in 4.0.0 may swallow its own IO errors** (the current source logs and returns). If it does, a save failure is invisible, and the backup archives the last successfully saved `app.config`. That is acceptable: the VM saves every change immediately.
- **R4: `ProgramData` ACLs.** A folder created under `C:\ProgramData` by one Windows user is read-only for other users. This is a single-user app, so it is out of scope, but it would surface as an "unwritable target" failure, which is handled.
- **R5: `CloseAll` while an index is in use.** There is no index use today, and the modal window blocks UI-initiated use. If background indexing (a periodic worker) is added later, it must pause during backup. Note this for that future ticket.

There are no decisions for the user.

## 9. Risky failure modes for QA

- A pooled SQLite connection keeps the staged DB open, so cleanup silently leaves `temporary\backup-*` behind. Assert that the folder is gone, not only that the result failed.
- `ZipFile.CreateFromDirectoryAsync` awaited without `Task.Run`, so the window freezes on a large index.
- An event published from inside a `Task.Run` body, which causes a cross-thread exception in `TaskRunnerWindowViewModel`.
- Cancel reported as a Failure (catch filter missing `OperationCanceledException`).
- Two backups in the same second, where the second overwrites the first.
- The live `app.db` gets created by the backup (opening the source connection read-write).
- The Success snackbar shown when the outcome is `Cancelled` (cancel during the final move window).
- Raw `[TaskRunnerWindow_...]` or `[PerformApplicationBackupRequestHandler_...]` text in pt-BR or en.

## 10. UX

**No UX spec.** This enables an existing card (IPX-0005 already matched the sample's layout) and reuses the `Nameless.Windows` TaskRunner window and snackbar (IPX-0008 §7). CLAUDE.md also says to skip `ux-specialist`. The only user-facing choices are the texts in 3.5, which are reviewed in the PR.

## 11. Why no ADR

The archive format is a new persisted contract, but it is not a database schema change, a framework change or an external integration. It follows the sample's pattern with `Nameless.Windows` primitives the project already uses, and it is recorded here (sections 2-3) for IPX-0011 to consume. The R1 follow-up, which moves the index storage root, is the one nearby change that may deserve an ADR, in its own ticket.

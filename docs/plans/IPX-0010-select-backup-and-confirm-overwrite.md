# IPX-0010: Select a backup and confirm overwrite

- Ticket: [IPX-0010 (#26)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/26)
- Epic: [IPX-0007 Backup & Restore (#23)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23), decisions D12 (Open-file dialog on the fixed folder, no managed list) and D16 (pre-restore overwrite warning).
- Builds on: [IPX-0009 plan](IPX-0009-create-application-backup.md) D1 (the fixed folder is `IApplicationContext.FileManager.GetBackupDirectory()`), D2 (file name pattern).
- Stack: .NET 10 / WPF (`dotnet`), one project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. There is no separate frontend.
- UX spec: **not needed**. See section 9.
- ADR: **none**. See section 10.

This ticket does no file I/O on backup content, runs no TaskRunner and sends no mediator request. It is a dialog-and-confirmation flow inside `AppConfigurationPageViewModel`, plus un-stubbing one card.

## 1. Context: what exists today (verified in this worktree and in Nameless.Windows 4.0.0)

| Concern | Where it really lives | Source |
|---|---|---|
| Restore card | `Views/Pages/AppConfigurationPage.xaml`: `ApplicationRestoreCard` has `IsEnabled="False"`, `ToolTip="{loc:Translator AppConfigurationPage_ComingSoon_Tooltip}"` and `ToolTipService.ShowOnDisabled="True"`. Its header holds a `StackPanel` with the title and `ApplicationRestoreBadge`. `ApplicationRestoreButton` has no `Command` and no `Cursor`. | XAML (IPX-0005 stub) |
| File dialog | `Nameless.Windows.Dialogs.FileSystem.IFileSystemDialog.OpenFile(Action<FileSelectionOptions>) : IEnumerable<string>`. `FileSelectionOptions` has `Title`, `Root`, `Owner` (from `FileSystemDialogOptions`) plus `Filter` and `Multiselect`. The implementation `FileSystemDialog` builds a `Microsoft.Win32.OpenFileDialog` with `DefaultDirectory = options.Root ?? FileManager.Root`, `ValidateNames = true`, owner falling back to the main window, and returns `dialog.FileNames`, which is **empty when the user cancels**. | `Nameless.Windows` source and 4.0.0 DLL (only `set_DefaultDirectory` is referenced, no `InitialDirectory`) |
| Message dialog | `Nameless.Windows.Dialogs.Message.IMessageDialog.Show(string, Action<MessageDialogOptions>) : MessageDialogResult`, with extension methods `ShowWarning(message, title = null, buttons = Ok, owner = null)` and `ShowQuestion(message, title = null, buttons = YesNo, owner = null)`. `MessageDialogResult` is `Ok, Cancel, Yes, No`. Any system result other than Cancel/Yes/No maps to `Ok`. There is **no default-button option**. | `Nameless.Windows` source, XML doc in 4.0.0 |
| DI registration | `FileSystemDialogHostFeature` and `MessageDialogHostFeature` (both `TryAddSingleton`) are part of `HostFeatureCollection.CreateDefault()`, which `App.xaml.cs` uses. `MainWindow` already resolves `IMessageDialog`. **No DI change needed.** | `AppHostFeatureCollectionExtensions` |
| Existing usage | `MainWindow.ClosingHandler` uses `_messageDialog.ShowQuestion(title:, message:, buttons: YesNoCancel)` with no owner. The reference sample's `PerformApplicationRestoreAsync` uses `_fileSystemDialog.OpenFile(o => { o.Title = ...; o.Filter = "Zip files (*.zip)\|*.zip"; }).FirstOrDefault()`. | Code |
| Backup folder | `IApplicationContext.FileManager.GetBackupDirectory()` returns an `IDirectory` (`Path`, `Exists`, `Create()`) for `{root}\backups`, today `C:\ProgramData\InfoPhoenix\backups\`. It is created on demand by the backup handler, so on a fresh install it does not exist yet. | IPX-0009 D1 |
| Test fakes | `FakeTranslator` echoes the key and **ignores the args**. `MainWindowTests` has a private `FakeMessageDialog`. There is no shared file/message dialog fake. The VM is built in 4 places across 3 test files. | `tests/.../Fakes`, test files |

## 2. Decisions

### D1: Use `IFileSystemDialog.OpenFile`, rooted at the backup folder, filtered to `*.zip`, single selection

```csharp
var backupDirectory = _applicationContext.FileManager.GetBackupDirectory();
EnsureExists(backupDirectory);                    // D2

var backupFilePath = _fileSystemDialog.OpenFile(options => {
    options.Title = T["AppConfigurationPage_Restore_SelectFile_Title"];
    options.Root = backupDirectory.Path;
    options.Filter = BuildArchiveFilter();       // "<localized name> (*.zip)|*.zip"
    options.Multiselect = false;
}).FirstOrDefault();
```

- **Filter:** only the description is localized. The pattern is a constant, so a translator cannot break the `name|pattern` syntax: `$"{T["AppConfigurationPage_Restore_SelectFile_FilterName"]} ({p})|{p}"` with `p = Constants.BackupRecovery.ArchiveFileFilterPattern = "*.zip"`. Do not narrow it to `InfoPhoenix-backup-*.zip`. That would hide a backup the user renamed or copied in from another machine (the epic's "machine change" goal).
- **Owner:** leave it `null`. `FileSystemDialog` falls back to the main window, which is the window the page lives in.
- **No other dialog type.** `IFileSystemDialog` is the `Nameless.Windows` primitive the sample uses. Wrapping `Microsoft.Win32.OpenFileDialog` directly was rejected, because CLAUDE.md says to prefer the package's primitives and the gain is small (see D3).

### D2: Create the backup folder before opening the dialog

If `{root}\backups` does not exist (fresh install, no backup yet), Windows silently ignores the default folder and the dialog opens somewhere unrelated, which breaks AC1. So the command calls `backupDirectory.Create()` (idempotent) first.

- This is the only filesystem effect of the command, and it happens before the user chooses anything. It creates an **empty folder that the backup feature creates anyway**. It does not touch any user data, so it does not count as a "state change" in AC3's sense ("no restore proceeds and no state changes" refers to the user's data and settings). QA should know it happens.
- If `Create()` throws (`IOException` or `UnauthorizedAccessException`, for example a file named `backups` in the way, or ACLs), log a warning and **still open the dialog** with the same `Root`. The user can still browse to a zip. Do not fail the command for this.

*Alternative:* don't create it, and let the dialog fall back. Rejected: on every fresh install AC1 would fail visibly the first time.

### D3: What "scoped to the fixed backup folder" means, given the dialog's real semantics

The standard Windows Open dialog **cannot restrict navigation** to one folder, and `Nameless.Windows` 4.0.0 sets `DefaultDirectory`, not `InitialDirectory`. In WPF, `DefaultDirectory` is used **only when Windows has no recently used folder** for this application's Open dialog. That gives this behavior:

| Situation | Where the dialog opens |
|---|---|
| First Restore click ever, or the user has only ever picked files from `backups\` | `backups\` |
| The user once browsed elsewhere and picked a file there | That other folder, on later opens (the Windows MRU folder for this exe) |

Nothing else in InfoPhoenix opens a file dialog today, so the MRU folder only moves away from `backups\` when the user deliberately picks a restore file from somewhere else. This plan treats AC1 as satisfied by "opens in the backup folder unless the user has since picked a backup from elsewhere". **QA must test AC1 on a clean MRU**, or by only ever picking from `backups\`.

Files picked **outside** `backups\` are **accepted**. The dialog cannot prevent it anyway, rejecting them would block the "copied from another machine" case, and IPX-0011 validates the archive content whatever its location.

*Alternatives, if the user wants strict "always opens in `backups\`" (non-blocking, see section 8, OD1):*
- (a) Add an "always start in `Root`" option (`InitialDirectory`) to `FileSystemDialog` in Nameless.Windows, and bump the package. Cost: a cross-repo change and a release. After that, InfoPhoenix sets one more option, a one-line change.
- (b) A local wrapper around `Microsoft.Win32.OpenFileDialog` with `InitialDirectory`. Cost: a new abstraction and a fake, and it goes against "prefer the Nameless.Windows primitives". Not recommended.

### D4: The overwrite warning is `IMessageDialog.ShowWarning(..., MessageDialogButtons.YesNo)`, and only `Yes` proceeds

```csharp
var confirmation = _messageDialog.ShowWarning(
    message: T["AppConfigurationPage_Restore_ConfirmOverwrite_Message", [SysPath.GetFileName(backupFilePath)]],
    title: T["AppConfigurationPage_Restore_ConfirmOverwrite_Title"],
    buttons: MessageDialogButtons.YesNo);

if (confirmation is not MessageDialogResult.Yes) { return; }   // AC3
```

- **Warning icon, not Question.** This is a destructive action, and the warning icon says so. `ShowWarning` takes a `buttons` argument, so it supports Yes/No.
- **Default-deny:** proceed only on exactly `Yes`. `No`, `Cancel` and `Ok` (the package maps any unexpected system result to `Ok`) all stop. A test covers each non-Yes value.
- **Name the file in the message** (file name only; the timestamp in the name identifies it), so the user confirms the right backup.
- **YesNo, not OkCancel:** the message ends with a direct question, and "Yes/No" answers it unambiguously.
- **Known limitation:** `MessageDialogOptions` has no default-button option, so Windows gives the first button (`Yes`) the focus, and pressing Enter confirms. The wording mitigates this: it states the consequence and ends with a question. A `No`-by-default option would need a Nameless.Windows change (section 8, OD2). That is not blocking for this ticket, because IPX-0011's restore is staged and atomic, and the user can back up first.

*Alternative:* a WPF-UI `ContentDialog` (`ContentDialogHostFeature` is registered). It gives styled buttons and a settable default button, but it needs a dialog host in `MainWindow.xaml` and async plumbing. That is more than this ticket needs, and `MainWindow` already uses `IMessageDialog` for its exit confirmation, so this stays consistent.

### D5: What happens after "Yes" in this ticket: log plus a temporary information snackbar, and a seam for IPX-0011

The ticket stops at confirmation. After `Yes`, the command:

1. Logs at Information: `"Restore confirmed for {BackupFilePath}; restore itself is not implemented yet (IPX-0011)."`
2. Publishes **one Information** `SnackBarNotificationEvent` with `T["AppConfigurationPage_Restore_NotYetAvailable_Message"]`, so a user who clicks Yes on a build between IPX-0010 and IPX-0011 is told honestly that nothing happened, and the confirmed branch is observable in tests.

IPX-0011 **replaces exactly these two statements** with the TaskRunner run (`PerformApplicationRestoreRequest(backupFilePath)`, IPX-0008 §3.3 shape), deletes the `NotYetAvailable` key from both `.resx` files and from `LocalizationFitnessTests`, and adds `await _pendingSave;` **after** confirmation and before the run (the restore overwrites `app.config`, so the IPX-0004 save chain must be drained first; the same reasoning as IPX-0009 D9). Nothing in this ticket awaits `_pendingSave`, because nothing here writes or reads config.

*Alternative:* a silent no-op after `Yes`. Rejected: the user would see nothing happen after confirming a destructive action, and the branch would have no observable effect to test.

### D6: No file-type or content validation here

`OpenFileDialog` checks that the file exists (its default `CheckFileExists = true`). The user could type a non-`.zip` name into the file-name box. This ticket does **not** check the extension or open the file: "does not touch any files" and "doesn't validate the zip" are explicit. IPX-0011 must handle "not a zip / corrupted / no manifest" as an anticipated failure anyway, so validating here would duplicate it. Recorded as an input for IPX-0011 (section 8).

## 3. Contract (binding for the developer)

### 3.1 ViewModel: `AppConfigurationPageViewModel`

Constructor: add `IFileSystemDialog fileSystemDialog` and `IMessageDialog messageDialog`, guarded with `Throws.When.Null` like the others, stored in `_fileSystemDialog` / `_messageDialog`. Put them before `ITranslator`, matching the sample's order; the exact position is the developer's call, as long as all 4 test construction sites are updated.

Command, generated as `PerformApplicationRestoreCommand` (the sample's name):

```csharp
/// <summary>
/// Restore entry point (IPX-0010): pick a backup, confirm the overwrite. IPX-0011 plugs the
/// actual restore in after the confirmation (plan D5).
/// </summary>
[RelayCommand]
private async Task PerformApplicationRestoreAsync() {
    try {
        var backupFilePath = SelectBackupFile();               // D1, D2

        if (backupFilePath is null) { return; }                // AC3: dialog cancelled

        if (!ConfirmOverwrite(backupFilePath)) { return; }     // AC2/AC3: D4

        // IPX-0010 stops here (D5). IPX-0011 replaces the next two statements.
        _logger.LogInformation(
            "Restore confirmed for {BackupFilePath}; restore itself is not implemented yet (IPX-0011).",
            backupFilePath);

        await _eventPublisher.PublishInformationAsync<SnackBarNotificationEvent>(
            message: T["AppConfigurationPage_Restore_NotYetAvailable_Message"]);
    }
    catch (Exception ex) {
        // Bug path only (for example a COM failure in the dialog). Never let it escape the
        // AsyncRelayCommand, where it would surface as an unhandled exception.
        _logger.LogError(ex, "Restore selection failed unexpectedly.");

        await _eventPublisher.PublishFailureAsync<SnackBarNotificationEvent>(
            message: T["AppConfigurationPage_Restore_Failure_Message", [ex.Message]]);
    }
}
```

- `SelectBackupFile()` is a private method returning `string?`: the D2 ensure-exists step (its own try/catch logs a warning and continues), then the D1 `OpenFile(...)` call, then `.FirstOrDefault()`. It treats a whitespace-only result as `null` too (`string.IsNullOrWhiteSpace`).
- `ConfirmOverwrite(string)` is a private method returning `bool`: D4.
- Both dialogs are synchronous and modal on the UI thread. **No `Task.Run`, no `ConfigureAwait(false)`.** The command is `async` only because of the snackbar publish.
- No busy flag: both dialogs are modal, so a second click cannot happen while they are open, and `AsyncRelayCommand` disables itself while running.
- **Must not** touch `_appConfigurationManager`, `_pendingSave`, `_mediator` or `_taskRunner` in any branch (AC3, "touches no files"). Tests enforce this (section 6).

### 3.2 View: `Views/Pages/AppConfigurationPage.xaml`, `ApplicationRestoreCard` (same pattern as IPX-0009 §3.4)

- `ApplicationRestoreCard`: remove `IsEnabled="False"`, `ToolTip="{loc:Translator AppConfigurationPage_ComingSoon_Tooltip}"` and `ToolTipService.ShowOnDisabled="True"`.
- Remove the `ApplicationRestoreBadge` element, and collapse the header's `StackPanel` into the title `ui:TextBlock` with `Grid.Row="0"`, exactly as `ApplicationBackupCard` looks now.
- `ApplicationRestoreButton`: add `Cursor="Hand"` and `Command="{Binding ElementName=AppConfigurationPageRoot, Path=ViewModel.PerformApplicationRestoreCommand, Mode=OneTime}"`. Keep `Appearance="Success"`, the icon and the caption.
- No command on the `CardControl` itself (same as every other card).
- Check-for-update stays stubbed, and so do the `AppConfigurationPage_ComingSoon_Badge` / `_Tooltip` keys, which `CheckForUpdateButton` / `CheckForUpdateBadge` still use.

### 3.3 Constants

Add `internal const string ArchiveFileFilterPattern = "*.zip";` to the existing `Constants.BackupRecovery`.

### 3.4 Localization keys: add to `Localization.resx` (en) and `Localization.pt-BR.resx`

The text is a suggestion, to be settled in PR review. The pt-BR text follows IPX-0009's choice of "backup".

| Key | en | pt-BR |
|---|---|---|
| `AppConfigurationPage_Restore_SelectFile_Title` | Select a backup to restore | Selecione um backup para restaurar |
| `AppConfigurationPage_Restore_SelectFile_FilterName` | InfoPhoenix backups | Backups do InfoPhoenix |
| `AppConfigurationPage_Restore_ConfirmOverwrite_Title` | Restore backup | Restaurar backup |
| `AppConfigurationPage_Restore_ConfirmOverwrite_Message` | Restoring "{0}" will replace all current application data (database, search index and settings) with the contents of this backup.<br><br>Your current data will be overwritten and cannot be recovered unless you have backed it up.<br><br>Do you want to continue? | Restaurar "{0}" substituirá todos os dados atuais da aplicação (banco de dados, índice de pesquisa e configurações) pelo conteúdo deste backup.<br><br>Os dados atuais serão sobrescritos e não poderão ser recuperados, a menos que você tenha feito um backup deles.<br><br>Deseja continuar? |
| `AppConfigurationPage_Restore_NotYetAvailable_Message` *(temporary, removed by IPX-0011)* | Restore confirmed, but restoring data is not available yet. Nothing was changed. | Restauração confirmada, mas a restauração de dados ainda não está disponível. Nada foi alterado. |
| `AppConfigurationPage_Restore_Failure_Message` | Could not start the restore: {0} | Não foi possível iniciar a restauração: {0} |

`<br><br>` in the table means a blank line: write real line breaks inside the `<value>` (the files already use `xml:space="preserve"`). Every pt-BR value differs from en, which `LocalizationFitnessTests` requires.

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function |
|---|---|---|
| Safety of a destructive action (AC2, AC3) | Two explicit gates. Only exactly `Yes` proceeds, and every other result stops. The file is named in the warning. | **F1:** a theory over `No`, `Cancel`, `Ok` asserting that nothing proceeds. **F2:** every cancel or decline test asserts zero published events, zero mediator requests and an empty `FakeAppConfigurationManager.CallLog`. |
| No side effects ("touches no files") | No mediator, no TaskRunner, no config access. The only filesystem effect is the idempotent creation of `backups\`. | The confirmed-path test asserts that the selected zip's bytes and `LastWriteTimeUtc` are unchanged and that no mediator request ran. |
| Operability / honest feedback | Information snackbar after `Yes` (D5). Failure snackbar on a bug path. Logs on every branch. | **F3:** extend `LocalizationFitnessTests.Keys` with the 6 keys in 3.4. |
| Evolvability (IPX-0011 plugs in) | One clearly marked seam with the selected path in scope. | None needed. Section 8 records the IPX-0011 hand-off. |

## 5. Security notes

- No new entry point beyond the local user's own button in a single-user desktop app.
- The path comes from the OS dialog (`ValidateNames = true`, existing file). This ticket only passes it to a log line and to `Path.GetFileName` for display. **IPX-0011 must treat the chosen zip as untrusted input**: zip-slip check on every entry, size limits, and the manifest validated before anything is swapped. Record this in IPX-0011's plan. It is not a rule for this ticket.
- The log line contains a local path. That is acceptable (IPX-0008 §5). `ex.Message` in the failure snackbar is likewise acceptable.

## 6. Test strategy

All of this ticket's logic is unit-testable with fakes. Both dialogs are interfaces, and the confirmed branch publishes through `IEventPublisher`.

**New shared fakes** (`tests/Nameless.InfoPhoenix.Client.Tests/Fakes/`):

- `FakeFileSystemDialog : IFileSystemDialog`. `OpenFile` invokes `configure` on a new `FileSelectionOptions`, stores it in `LastFileSelectionOptions`, increments `OpenFileCount`, then either throws `ThrowOnOpenFile` (if set) or returns `FilesToReturn` (default `[]`, which means cancelled). `OpenDirectory` / `OpenSave` throw `NotSupportedException`.
- `FakeMessageDialog : IMessageDialog`. `Show` invokes `configure` on a new `MessageDialogOptions`, records `(message, options)` in `Calls`, and returns `ResultToReturn` (default `No`, which is safe by default). Leave the private one in `MainWindowTests` alone: moving it is outside this ticket's scope.
- `FakeTranslator`: **additive change.** Record every indexer call in `public List<(string Key, object[] Args)> Requests`, so a test can assert the file name was passed as `{0}`. The returned value stays the bare key, so existing tests are unaffected.

**VM tests** (`ViewModels/Pages/AppConfigurationPageViewModelTests.cs`, new region `PerformApplicationRestore`). Use the existing `StaWindowTestHarness` + `CreateViewModel` pattern, extended with optional `fileSystemDialog` / `messageDialog` parameters, and a `FakeApplicationContext` whose `FileManager` is `new PhysicalFileManagerFactory().GetOrCreate(tempRoot)` (a per-test temp folder, deleted afterwards, as in the backup handler tests). Run the command with `await vm.PerformApplicationRestoreCommand.ExecuteAsync(null)`. All fakes complete synchronously.

| AC | Case | Assert |
|---|---|---|
| AC1 | Click with no `backups\` folder yet | `OpenFileCount == 1`. `Root == {tempRoot}\backups`, and that folder now exists. `Filter` ends with `"|*.zip"` and contains `"(*.zip)"`. `Multiselect == false`. `Title == "AppConfigurationPage_Restore_SelectFile_Title"`. |
| AC1 / D2 | A **file** named `backups` blocks `Create()` | The dialog still opens with the same `Root`. No exception. No failure snackbar. |
| AC3 | The file dialog returns `[]` (cancelled) | `FakeMessageDialog.Calls` is empty. No events published. `FakeMediator.ExecutedRequests` is empty. `manager.CallLog` is empty. |
| AC2 | The file dialog returns `{tempRoot}\backups\InfoPhoenix-backup-20261105T143012Z.zip` (a real file created by the test) | Exactly one message dialog call. `Icon == MessageDialogIcon.Warning`, `Buttons == YesNo`, `Title == "..._ConfirmOverwrite_Title"`. The translator saw `..._ConfirmOverwrite_Message` with `Args == ["InfoPhoenix-backup-20261105T143012Z.zip"]`. |
| AC3 | Theory: the warning returns `No`, `Cancel`, `Ok` | No events. No mediator requests. `CallLog` is empty (F1, F2). |
| AC2 (stop point) | The warning returns `Yes` | Exactly one `SnackBarNotificationEvent` of type Information with `..._NotYetAvailable_Message`. No mediator requests. `CallLog` is empty. The selected zip's bytes and `LastWriteTimeUtc` are unchanged. |
| AC2 | A file picked **outside** `backups\` | Same as the AC2 row: the warning is shown (D3, accepted). |
| Robustness | `ThrowOnOpenFile = new InvalidOperationException("boom")` | No exception escapes. Exactly one Failure snackbar with `..._Restore_Failure_Message`. |

**Component tests** (`Views/Pages/AppConfigurationPageTests.cs`, STA):

- Remove `"ApplicationRestoreCard"` from `StubElement_IsDisabledAndHasNoCommand` and from `StubElement_HasTooltipThatShowsWhileDisabled`, and remove `"ApplicationRestoreBadge"` from `StubBadge_ContentResolvesThroughTranslator`. Each keeps its `CheckForUpdate*` row.
- **Delete** `StubCardInnerButton_HasNoCommandAndIsDisabled`. Its only row is `ApplicationRestoreButton`, and a theory with no data fails in xUnit. Update the class summary comment that still says "Backup and Restore cards ... stubbed".
- Add `ApplicationRestoreCard_IsEnabled_WithButtonBoundToARealCommand`, mirroring the backup one: the card is enabled, the button's `Command` is not null, the card has no `ToolTip`, and `page.FindName("ApplicationRestoreBadge")` is null.
- Every `new AppConfigurationPageViewModel(...)` (2 here, 1 in `AppConfigurationPageViewModelIntegrationTests`, 1 in `AppConfigurationPageViewModelTests`) gets `new FakeFileSystemDialog()` and `new FakeMessageDialog()`.

**Fitness:** add the 6 keys from 3.4 to `LocalizationFitnessTests.Keys` under a `// IPX-0010 §3.4` comment (F3).

**Manual check only** (once, exploratory). These need the real Windows dialogs:

1. On a fresh data root (no `backups\`), click Restore: the dialog opens **in** `...\InfoPhoenix\backups\` and lists only `.zip` files under the localized filter name.
2. Cancel: nothing happens. Pick a zip, then click No: nothing happens. Pick a zip, then click Yes: an info snackbar appears, and no file changes in the data root (compare timestamps).
3. The warning shows the warning icon, the right file name, line breaks rendered, and Yes/No buttons, and it is modal over the main window (it is not hidden behind it).
4. The pt-BR texts in both dialogs (the dialog's own buttons follow the Windows UI language, not the app language; that is expected).
5. D3 behavior: pick a file from another folder once, then reopen. The dialog now starts in that folder. That is expected and documented.

## 7. Work split and developer tasks (in build order)

**BACKEND-ONLY.** One developer, `backend-developer-dotnet`.

1. Add `Constants.BackupRecovery.ArchiveFileFilterPattern` (3.3). Add the 6 keys to both `.resx` files and extend `LocalizationFitnessTests.Keys` (F3). (AC1, AC2)
2. Add `FakeFileSystemDialog` and `FakeMessageDialog`, and the additive `Requests` recording in `FakeTranslator`. (test infra)
3. Inject `IFileSystemDialog` / `IMessageDialog` into `AppConfigurationPageViewModel`, and update the 4 construction sites. (AC1, AC2)
4. Implement `PerformApplicationRestoreCommand` per 3.1 / D1-D5, together with the VM tests in section 6, TDD-style, starting with the AC3 cancel and decline cases. (AC1, AC2, AC3)
5. Un-stub the card (3.2), update and delete the IPX-0005 stub tests, and add the enabled/bound test. (AC1)
6. Do the manual pass (section 6). Add a CHANGELOG `Added` entry: "Restore button: pick a backup from the backup folder and confirm the overwrite; the restore itself arrives with IPX-0011."

## 8. Risks, open points, hand-off to IPX-0011

- **OD1 (non-blocking, user may override): strict "always opens in `backups\`".** As designed, the dialog opens in `backups\` unless the user has since picked a restore file elsewhere (D3, a Windows MRU behavior of `DefaultDirectory`). Recommendation: accept it for now. If strict behavior is wanted, add an `InitialDirectory`-style option to Nameless.Windows `FileSystemDialog` (option (a) in D3), which then costs a one-line change here.
- **OD2 (non-blocking): default button on the warning is `Yes`.** `MessageDialogOptions` cannot set a default result. Recommendation: accept it, given the wording and IPX-0011's atomic restore. If wanted, a `DefaultResult` option in Nameless.Windows is a small additive package change.
- **R1: the temporary `NotYetAvailable` snackbar ships if a release is cut between IPX-0010 and IPX-0011.** That is acceptable: it is honest and says nothing changed. IPX-0011 removes the key (D5).
- **R2: system dialog buttons follow the Windows display language, not the app language.** A pt-BR app on an English Windows shows "Yes/No". This is inherent to `MessageBox` and not fixable via `IMessageDialog`. Do not file it as a bug.
- **Hand-off to IPX-0011** (put these in its plan):
  - (a) Replace the two D5 statements with the TaskRunner run, and remove the `NotYetAvailable` key and its fitness entry.
  - (b) `await _pendingSave;` after confirmation, before the run.
  - (c) Treat the zip as untrusted: no extension check here (D6), so "not a zip", "no manifest" and "corrupted" are anticipated failures there. Also zip-slip and size limits.
  - (d) The path may be outside `backups\` (D3).

There are no blocking decisions for the user.

## 9. UX

**No UX spec.** This enables an existing card, and it uses the standard Windows Open dialog and a standard warning message box through existing `Nameless.Windows` abstractions, the same mechanism as `MainWindow`'s exit confirmation. CLAUDE.md also says to skip `ux-specialist`. The one real UX stake is the destructive-action wording. It is handled in D4 / 3.4 (states the consequence, names the file, suggests backing up first, ends with a question, warning icon, default-deny) and is reviewed in the PR. The default-button limitation is recorded as OD2.

## 10. Why no ADR

This is a routine feature addition that follows existing patterns: two already-registered `Nameless.Windows` services, and a command shaped like the sample's. It involves no framework or structural change, no external API, and no schema or data change.

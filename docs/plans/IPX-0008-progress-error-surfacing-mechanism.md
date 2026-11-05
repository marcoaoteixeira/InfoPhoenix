# IPX-0008: Progress and error surfacing for Backup/Restore (spike)

- Ticket: [IPX-0008 (#24)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/24), spike, time-boxed to half a day.
- Epic: [IPX-0007 Backup & Restore (#23)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23). This spike resolves the open assumption in epic decision **D8** ("TaskRunner window ... confirm it's available/appropriate").
- Unblocks: IPX-0009 (#25, create a backup) and IPX-0011 (#27, atomic restore). IPX-0012 (#28, tests) also depends on it indirectly.
- Stack: .NET 10 / WPF (`dotnet`), one project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. There is no separate frontend.
- UX spec: **not needed**. See section 7.
- ADR: **none**. The decision is recorded in section 1 of this plan, and IPX-0009/IPX-0011 refer to it from there. See section 8 for why there is no ADR.
- Production code: **none**. The `Nameless.WinApp` sample already does Backup and Restore through this primitive, so a throwaway proof would add nothing.

## 1. Decision

**Backup and Restore surface progress and errors through the `Nameless.Windows` TaskRunner window. InfoPhoenix does not build its own progress window or dialog.**

The ViewModel starts each operation with `ITaskRunner.CreateBuilder().SetName(...).SubscribeFor<BackupRecoveryProgressNotificationEvent>().SetDelegate(ct => _mediator.ExecuteAsync(request, ct)).RunAsync()`. The handler reports each step by publishing `BackupRecoveryProgressNotificationEvent` through `IEventPublisher` (`PublishInformationAsync`/`PublishWarningAsync`/`PublishFailureAsync`/`PublishSuccessAsync`). The window shows those messages as a colour-coded log, with an indeterminate progress ring and a Cancel button while the operation runs.

When the operation fails, the handler cleans up, publishes one localized **Failure** entry, and returns a non-success result. That entry is how the error is "surfaced via the confirmed mechanism". The window stays open, with only **Close** enabled, until the user dismisses it. After `RunAsync` returns, the ViewModel looks at the outcome and the handler result:

- A successful backup gets a Success snackbar that names the file.
- A successful restore gets the relaunch prompt from `IMessageDialog`.
- Any other result needs no further action.

An unexpected exception still reaches the ViewModel, rethrown by `await RunAsync()`. The ViewModel logs it and shows a Failure snackbar.

All of this is already in the referenced `Nameless.Windows` 4.0.0 package and is registered by `WinHostFactory.CreateDefault`. InfoPhoenix needs **no host or DI changes**. The only additions are the translator keys listed in section 3.4.

## 2. Findings

Everything below was checked against the installed package (`~/.nuget/packages/nameless.windows/4.0.0`: XML docs and README) and against the matching source in the local Nameless repo (`libs/windows/src/Nameless.Windows/TaskRunner/`, `Notification/`). The packaged README is byte-identical to the source README, and the XML docs match the `Task<TaskRunnerOutcome> RunAsync()` signature.

| Question | Answer |
|---|---|
| Does a reusable progress/error window exist? | **Yes.** `Nameless.Windows.TaskRunner`: `ITaskRunner` → `TaskRunnerBuilder` → modal `TaskRunnerWindow`. |
| Is it registered in InfoPhoenix today? | **Yes, with no extra code.** `TaskRunnerHostFeature`, `EventPublisherHostFeature`, `SnackBarHostFeature`, `MessageDialogHostFeature` and `WindowHostFeature` are all part of `HostFeatureCollection.CreateDefault()`, which `App.xaml.cs` already uses. `WinHostFactory` always adds the `Nameless.Windows` assembly to the scan, so `ITaskRunnerWindow`/`TaskRunnerWindow` are resolvable. |
| Is there an event type for backup/restore progress? | **Yes.** `Nameless.Windows.Notification.BackupRecovery.BackupRecoveryProgressNotificationEvent` (a `NotificationEvent` record). The library ships it for exactly this purpose. |
| Does the reference sample use it for this? | **Yes.** `Nameless.WinApp` `AppConfigurationPageViewModel.PerformApplicationBackupAsync` / `PerformApplicationRestoreAsync`, with handlers in `Features/PerformApplicationBackup/` and `Features/PerformApplicationRestore/`. |
| Is snackbar delivery wired in InfoPhoenix? | **Yes.** IPX-0006 added `MainWindow`'s `Register<SnackBarNotificationEvent>`. |

How the window behaves (from source, `TaskRunnerWindowViewModel` / `TaskRunnerWindow` / `TaskRunnerBuilder`):

- **Modal, owned by `MainWindow`, centred on the screen.** The user cannot start a second operation or interact with the main window while it is open.
- **It runs the delegate when it loads.** While it runs: an indeterminate `ProgressRing`, the title, and a log (`LoggerRichTextBox`). Notification types map to log styles: Information → info, Success → success, Warning → warning, Failure → error.
- **Cancel** is enabled only while the run is active. It cancels the `CancellationToken` passed to the delegate. **Close** is enabled only when the run is idle. Title-bar close and Alt+F4 are **blocked** until the run completes, and a one-time `TaskRunnerWindow_CloseBlocked_Message` warning is shown.
- **`RunAsync()` completes only after the user closes the window** (`ShowDialog` inside `Dispatcher.InvokeAsync`). It returns `TaskRunnerOutcome.Completed` or `TaskRunnerOutcome.Cancelled`. It **faults** with the delegate's exception unless that exception is an `OperationCanceledException` for the run's own token, which becomes `Cancelled` plus a `TaskRunnerWindow_Cancelled_Message` warning entry. When the delegate throws, the window also adds an Error entry containing `ex.Message`.
- **Each builder runs once.** Create a new builder for every run.
- **Threading.** The delegate is started from the window's `Loaded` handler on the UI dispatcher, so its `await` continuations come back to the UI thread. Synchronous CPU or IO work inside it **freezes the window**, including the ring and the Cancel button, unless it is wrapped in `Task.Run`. Never block on `RunAsync()` with `.Wait()` or `.Result`, because that deadlocks.

There is **no determinate percentage bar**. `NotificationEvent` has no percentage field, and the ring is indeterminate. That is acceptable here: the epic asks for progress and error surfacing for a multi-second operation, not a percentage. Step-by-step log lines serve that purpose.

## 3. Contract for IPX-0009 and IPX-0011

These rules bind both tickets. Report any deviation in that ticket's plan.

### 3.1 Feature slices (following the sample)

| Ticket | Request | Handler | Result |
|---|---|---|---|
| IPX-0009 | `Features/PerformApplicationBackup/PerformApplicationBackupRequest : IRequest<PerformApplicationBackupResult>` | `PerformApplicationBackupRequestHandler` | `sealed record PerformApplicationBackupResult(string? BackupFilePath)` with `bool Succeeded => BackupFilePath is not null` |
| IPX-0011 | `Features/PerformApplicationRestore/PerformApplicationRestoreRequest(string SourceZipFilePath) : IRequest<RestoreOutcome>` | `PerformApplicationRestoreRequestHandler` | `enum RestoreOutcome { Completed, Blocked, RolledBack }`: `Blocked` = validation or version mismatch, nothing touched; `RolledBack` = staging or swap failed, prior state restored |

IPX-0009 and IPX-0011 own the internal names and the archive and marker formats. IPX-0011 may refine the `RestoreOutcome` values. The **ViewModel rule does not change** if it does: only `Completed` leads to the relaunch prompt.

Why the backup result differs from the sample: the sample's backup handler **throws** on failure, while its restore handler **returns** an outcome. That inconsistency means the failure shows twice in the window (the handler's localized Failure entry plus TaskRunner's raw `ex.Message` entry), and the ViewModel needs two failure paths. InfoPhoenix uses the result-returning style for both handlers.

### 3.2 Handler rules (where the logic lives)

1. **Report progress only through `BackupRecoveryProgressNotificationEvent`.** Use `_eventPublisher.Publish{Information|Warning|Failure|Success}Async<BackupRecoveryProgressNotificationEvent>(message: T[...])`. Do not create a new event type. Use one Information entry per step (for example: saving settings, copying database, copying search index, creating archive / extracting, validating, swapping).
2. **Anticipated failure** (IO, disk full, access denied, invalid or mismatched archive, and so on):
   - catch it, using **specific exception types** (`IOException`, `UnauthorizedAccessException`, `InvalidDataException`, ...) or `catch (Exception ex) when (ex is not OperationCanceledException)`. **Never use a bare `catch (Exception)`.** It would also catch the `TaskCanceledException` that `Task.Run(..., ct)` throws after Cancel is pressed, so a cancel would be reported as a failure. That breaks rule 4 and test F2;
   - **clean up first** (IPX-0009: partial zip and temp/staging folder; IPX-0011: staging folder, and roll back any swap that already happened);
   - publish **exactly one** localized Failure entry that names the step and includes the reason;
   - return the failure result (`BackupFilePath = null` / `Blocked` / `RolledBack`).
   - **Do not rethrow.**
3. **Success**: publish one Success entry. For backup, that entry names the created file (IPX-0009 AC1, inside the window). Then return the success result.
4. **Cancellation** (Cancel button): clean up, then let the `OperationCanceledException` from the run's token **propagate**. TaskRunner reports it as `Cancelled` and logs its own warning. Do not also publish a Failure entry.
   - **IPX-0011 exception:** once the swap starts (the commit point), the handler must **stop observing the token**. Do not pass it into the swap or anything after it. A cancel must never interrupt a half-done swap.
5. **Offload synchronous work** (directory copies, `SqliteConnection.BackupDatabase`, file moves) with `await Task.Run(..., ct)`. **Publish events between steps from the awaiting flow, not from inside `Task.Run` bodies.** The window then stays responsive and the log updates in order. The main reason, though, is that **events must be published on the UI thread**. `InProcessEventPublisher` delivers synchronously, and `TaskRunnerWindowViewModel.Update` mutates a UI-bound `ObservableCollection` without dispatching, so publishing from a background thread crashes at runtime. For the same reason, **never use `ConfigureAwait(false)` or `SkipContextSync()` in these handlers.** Every `await` must resume on the UI context.
6. **Do not publish snackbars from handlers.** Snackbars belong to the ViewModel. This differs from the sample's restore handler, which publishes a snackbar itself.

### 3.3 ViewModel shape (`AppConfigurationPageViewModel`)

Inject `ITaskRunner` (and, for IPX-0010/0011, `IMessageDialog` / `IFileSystemDialog`). Keep each command thin:

```csharp
[RelayCommand]
private async Task PerformApplicationBackupAsync() {
    PerformApplicationBackupResult? result = null;

    try {
        var outcome = await _taskRunner.CreateBuilder()
            .SetName(T["AppConfigurationPage_Backup_TaskRunnerWindow_Title"])
            .SubscribeFor<BackupRecoveryProgressNotificationEvent>()
            .SetDelegate(async ct => result = await _mediator.ExecuteAsync(new PerformApplicationBackupRequest(), ct))
            .RunAsync();

        if (outcome is TaskRunnerOutcome.Completed && result is { Succeeded: true }) {
            await _eventPublisher.PublishSuccessAsync<SnackBarNotificationEvent>(
                message: T.Get("...Backup_Success", result.BackupFilePath));
        }
        // Failed or Cancelled: already shown in the window, nothing more to do.
    }
    catch (Exception ex) {
        // Unexpected bug path only (anticipated failures do not throw, rule 3.2.2).
        _logger.LogError(ex, "Backup failed unexpectedly.");
        await _eventPublisher.PublishFailureAsync<SnackBarNotificationEvent>(message: T.Get("...Backup_Failure", ex.Message));
    }
}
```

On the unexpected-exception path, the same message appears twice: once in the TaskRunner log (the window's own `Error(ex.Message)` entry) and again in the ViewModel's Failure snackbar after the window closes. **This is intended, not a duplicate-display bug.** The log entry explains the failure while the window is open, and the snackbar confirms the failure on the main window after the user closes it. The path is reserved for bugs, so it should be rare.

Restore (IPX-0011) has the same shape. After `RunAsync`, **only** `outcome is Completed && restoreOutcome is RestoreOutcome.Completed` leads to the relaunch prompt (`_messageDialog.ShowQuestion(...)`, epic D14: prompt, never automatic). The file pick and overwrite confirmation (IPX-0010) happen **before** the builder is created.

Rules:
- Always `await RunAsync()`. Never `.Wait()` or `.Result`.
- Use a new builder per click. The modal window already prevents re-entrancy, so no "busy" flag is needed.
- Do not put backup or restore logic in the ViewModel. `ITaskRunner.CreateBuilder()` returns the concrete `TaskRunnerBuilder`, which needs a real WPF dispatcher, so the ViewModel's run path **cannot be unit-tested**. That is the reason for putting all logic in the handler.

### 3.4 Translator keys (required: InfoPhoenix's `ResourceTranslator` shows missing keys as `[key]`)

IPX-0009 adds these to **both** `Resources/l10n/Localization.resx` and `Localization.pt-BR.resx`, because it is the first ticket to use the window:

| Key | en | pt-BR (suggested) |
|---|---|---|
| `TaskRunnerWindow_CancelButton_Caption` | Cancel | Cancelar |
| `TaskRunnerWindow_CloseButton_Caption` | Close | Fechar |
| `TaskRunnerWindow_Cancelled_Message` | Operation cancelled. | Operação cancelada. |
| `TaskRunnerWindow_CloseBlocked_Message` | Please wait until the operation finishes before closing this window. | Aguarde a conclusão da operação antes de fechar esta janela. |

Each ticket adds its own window title, step, failure and success keys. They follow the existing `AppConfigurationPage_*` naming, and IPX-0009/0011 settle the exact text.

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function (suggested, cheap) |
|---|---|---|
| Operability / user feedback | Every step and the failure reason appear in a modal log that cannot be closed mid-run, and errors stay visible until the user dismisses them. | F1: extend the existing `tests/Nameless.InfoPhoenix.Client.Tests/Views/Pages/LocalizationFitnessTests.cs` so it also asserts that the four `TaskRunnerWindow_*` keys from 3.4 resolve to non-empty values in both `Localization.resx` and `Localization.pt-BR.resx`. Do not create a new test file. Lands with IPX-0009. |
| Consistency / safety (no partial state) | Cleanup happens inside the handler before it returns, and restore ignores Cancel after the commit point. | Covered by IPX-0012's byte-for-byte tests, run against the handlers directly. |
| Testability | All logic sits in the Mediator handlers, which are testable with the existing `FakeEventPublisher` and `FakeTranslator`. Tests assert the **published Failure/Success entries** and the **returned result**. | F2 (IPX-0012): every failure test asserts exactly one `NotificationType.Failure` `BackupRecoveryProgressNotificationEvent` was published, which enforces rule 3.2.2. |
| Responsiveness | Synchronous IO is offloaded (rule 3.2.5). | Manual check only. No cheap automated check exists for UI-thread blocking. |

## 5. Security notes

- No new entry point beyond the local user's own button clicks. Single-user desktop, no authentication or authorization surface.
- Failure messages include `ex.Message`, which can contain **local file paths**. That is acceptable for a local single-user app (the user already owns those paths). Do not add more detail, such as stack traces, to the window. Stack traces go to the Serilog log only.
- Restore input (the chosen zip) is untrusted file content. Zip-slip and archive validation belong to IPX-0011; this spike only sets the rule that a rejected archive returns `Blocked` with one Failure entry.

## 6. Test strategy (for qa-engineer)

| What | Level | Notes |
|---|---|---|
| Handler publishes Information per step, then one Success (backup: names the file) | Unit (handler + `FakeEventPublisher`) | IPX-0009 / IPX-0011 |
| Handler failure → cleanup done, exactly one Failure entry, failure result, **no throw** | Unit / integration against a temp folder | IPX-0012 |
| Cancellation → `OperationCanceledException` propagates, cleanup done, no Failure entry | Unit | IPX-0009 (backup), IPX-0011 (before the commit point only) |
| Restore ignores cancellation after the commit point | Unit | IPX-0011 |
| Window shows the steps, Close disabled while running, Alt+F4 blocked, Failure line visible, snackbar after close | Manual exploratory | Not automatable (real dispatcher). One pass per ticket. |
| All four `TaskRunnerWindow_*` keys resolve in en and pt-BR | Unit (F1) | IPX-0009 |

Risky failure modes worth probing:
- The window freezes (ring stops, Cancel does nothing) because a synchronous copy runs on the UI thread.
- The error appears twice (the handler both publishes and throws).
- Raw `[TaskRunnerWindow_...]` text on the buttons (missing keys).
- Cancel pressed during the restore swap.
- A non-success outcome still triggers the relaunch prompt.

## 7. UX

**No UX spec.** The window, its layout, its buttons and its behaviour come from `Nameless.Windows` and cannot be changed from InfoPhoenix. CLAUDE.md also says to skip `ux-specialist` for this project. The only user-visible decisions are **wording**:
- the window titles and step, failure and success messages (owned by IPX-0009 and IPX-0011, reviewed in their PRs);
- the four generic keys above (suggested text is given).

No new dialog or window is introduced. The relaunch prompt (IPX-0011) and the overwrite warning (IPX-0010) reuse the existing `IMessageDialog`.

## 8. Why no ADR

The ADR categories are structural framework change, external API integration and database mutation. This decision is none of them. It adopts a component of a package the project **already references and already registers**, following the reference sample's established pattern, with no new dependency, layering or storage change. This plan records the decision (the ticket AC asks for "a short note ... or a one-paragraph ADR note"), and IPX-0009/IPX-0011 link to section 1 and section 3 of this file.

## 9. Work split and tasks

**BACKEND-ONLY.** This spike produces no code. The tasks below belong to the downstream tickets and are listed here so their plans can reference them:

- IPX-0009:
  1. Add the four `TaskRunnerWindow_*` keys (en and pt-BR), and extend `LocalizationFitnessTests.cs` with them (F1).
  2. Add the backup slice and result per 3.1/3.2.
  3. Add the backup command per 3.3 and enable the card.
- IPX-0011:
  1. Add the restore slice and `RestoreOutcome` per 3.1/3.2, including the commit-point cancellation rule.
  2. Wire the restore command per 3.3, including the relaunch prompt.
- IPX-0012: tests per section 6, including F2.

## 10. Risks and open points

- **No percentage progress.** If a percentage bar is ever wanted, that is a `Nameless.Windows` change, not an InfoPhoenix one. It is out of scope for this epic.
- **The ViewModel run path cannot be unit-tested** (the concrete `TaskRunnerBuilder` needs a dispatcher). The sample accepts the same limit. Mitigation: keep the command as thin as in 3.3 and cover it manually. Wrapping `ITaskRunner` in an InfoPhoenix interface was considered and rejected: it is an abstraction this epic does not need (CLAUDE.md "no premature abstraction").
- **The Cancel button is always available during restore.** Mitigated by rule 3.2.4 (ignore the token after the commit point). IPX-0011 must test it.
- **There are no open decisions for the user.** The primitive exists and matches what the product owner already chose in epic D8.

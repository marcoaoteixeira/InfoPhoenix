# IPX-0006: App configuration invalid-state recovery

- Ticket: [IPX-0006-AppConfigurationInvalidStateRecovery (#15)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/15)
- Epic: [#11 Setup & Initialization](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11) (decision D9, open question Q2)
- Depends on: IPX-0004 (#13, merged). Builds on `docs/plans/IPX-0004-app-configuration-page-live-settings.md`.
- Stack: .NET 10 / WPF (`dotnet`), single project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. There is no separate frontend; the WPF UI is part of the same .NET project.
- UX spec: **not needed**. The ticket adds no screen and no new state. It reuses the existing WPF-UI snackbar (`SnackBarNotificationEvent`, Failure appearance) and returns the page to its normal state. The message text is fixed in section 2.6.
- ADR: **none**. There is no framework change and no new external integration. There is also no schema or data-model change: `app.config` keys and format stay as they are, owned by `Nameless.Windows`.
- Status: **one open decision (D-1, section 9)**. It does not block the work: the tasks below are the same under the recommended option.

## 1. Context: what "invalid or unreadable" actually means here

The epic asks the architect where "invalid/unreadable" is detected, inside `IAppConfigurationManager` or in the ViewModel (epic tickets, IPX-0006 "Notes for team"). The answer comes from `Nameless.Windows` 4.0.0 (`libs/windows/src/Nameless.Windows/AppConfiguration/AppConfigurationManager.cs`, tag `lib-v4.0.0`):

| Situation on disk | What the real `AppConfigurationManager` does | Visible to InfoPhoenix through the interface? |
|---|---|---|
| No `app.config` (first run) | Empty dictionary. | Every key is absent, so the getters return defaults. **This is the happy path.** |
| `app.config` is `{}` (written by the shutdown save when nothing changed) | Empty dictionary. | The same as above. **Happy path.** |
| Malformed JSON (truncated or garbage) | `JsonSerializer.Deserialize` throws. The manager **catches and logs** the error and returns an empty dictionary. | **No.** It looks exactly like a first run, because the interface has no load-status member. See D-1. |
| File present but cannot be opened (locked by another process, ACL denied) | `file.Open()` sits **outside** the try block, so an `IOException`/`UnauthorizedAccessException` escapes from the lazy `AppConfiguration` getter on the **first** `TryGet`/`Set`. The backing field stays `null`, so **every later access retries and throws again** until the file becomes readable. | **Yes**, as an exception from any `TryGet`/`Set` (and so from the `Theme`/`Language`/`ConfirmBeforeExit` extension getters). |
| Key present with the wrong JSON type (`"Theme": "Dark"`, `"ConfirmBeforeExit": "yes"`, `"UILanguage": 5`) | `TryGet<T>` catches the deserialisation error, logs it, and returns `false`. The extension getter returns the default. | Only by **probing for presence**: `TryGet<object>(key)` is `true` while `TryGet<T>(key)` is `false`. |
| Key present, right type, unsupported value (`"Theme": 42`, `"UILanguage": "fr-FR"` or `"xx"`) | Returned as is. | **Yes.** Today this produces a **blank combo box**: `Theme.Find`/`CultureInfo.Find` return `ComboBoxItemHelper.EmptyComboBoxItem` (the "stuck/blank state" in AC2). |

Who reads the manager today, and what happens when it throws:

| Reader | When | Effect of a throwing manager today |
|---|---|---|
| `MainWindow.InitializeWindow` reads `Theme` | MainWindow constructor, before the splash | **The app crashes at startup.** The page is never reachable. |
| `SetUILanguageStep.ExecuteAsync` reads `Language` | Bootstrap | The step fails, so the bootstrapper raises a `BootstrapException`. |
| `MainWindow.ClosingHandler` reads `ConfirmBeforeExit` and, on "No", sets it | Every close | `async void`, so an exception there crashes the process. |
| `AppConfigurationPageViewModel.Initialize` and `On<X>Changed` | Page navigation and each user change | The exception escapes navigation or the binding setter. |
| `App.OnCurrentHostStop` calls `SaveChangesAsync` | Exit | The real manager's save catches everything (the `AppConfiguration` getter is inside its try block). No change is needed. |

**Second finding (a latent IPX-0004 bug, required for AC1):** InfoPhoenix's `MainWindow` calls `_snackBarService.SetSnackbarPresenter(...)` but **never subscribes** `SnackBarNotificationEvent` to `ISnackbarService`. The sample does this in `MainWindow.SubscribeForNotifications()` (`_eventPublisher.Register<SnackBarNotificationEvent>(this, ...)` → `Dispatcher.InvokeAsync(() => _snackBarService.Show(evt))`). Nothing in `Nameless.Windows` subscribes it for you: `SnackBarHostFeature` only registers `ISnackbarService`. As a result, **no snackbar published through `IEventPublisher` is ever shown in the running app today**, including IPX-0004's "missing log file" warning. This ticket must port the subscription, otherwise AC1 passes in unit tests and fails for the user.

## 2. Approach

### 2.1 Where recovery lives (answers the epic's question)

- **Detection and recovery: in `AppConfigurationPageViewModel.Initialize`**, the page's "load settings" moment. This is the ticket's stated scope ("the live-settings page/ViewModel this recovery wraps", "without crashing the configuration page"). The test seam is the existing `FakeAppConfigurationManager` (AC4).
- **No-crash hardening, with no snackbar and no reset, in the three startup/close readers** (`MainWindow.InitializeWindow`, `MainWindow.ClosingHandler`, `SetUILanguageStep`). Without it, a throwing manager crashes the app before the page can ever recover anything, which contradicts the ticket's "or the app". Each reader falls back to the default for that one read and logs.
- **Snackbar delivery:** port the sample's `SubscribeForNotifications` into `MainWindow`.

**Alternative considered: recover once at startup** (a bootstrap step or `MainWindow.InitializeWindow`, before any reader), with the snackbar on launch.
- Pros: every consumer sees valid settings, and the user is told even if they never open the settings page.
- Cons: it moves the behaviour out of the page the ticket wraps, and the snackbar's fixed 5 s timeout runs while the splash covers the main window, so the user would likely miss it. It also needs a new cross-cutting service.
- **Rejected for now** (assumption A1, section 9). If the owner wants it later, the detection routine in 2.3 can move into a shared `Infrastructure/` type unchanged.

**Alternative considered: detect inside the manager.** That is not possible without a `Nameless.Windows` change, because the interface has no load-status member. This is D-1.

### 2.2 Files

All paths are under `src/Nameless.InfoPhoenix.Client/` unless stated otherwise.

| Path | Change | AC |
|---|---|---|
| `Constants.cs` | Add `AppConfiguration.Keys` (`Theme = "Theme"`, `Language = "UILanguage"`, `ConfirmBeforeExit = "ConfirmBeforeExit"`, which mirror `Nameless.Windows`' private key names) and `AppConfiguration.Defaults` (`Theme.System`, `"pt-BR"`, `false`, which mirror the extension getters' defaults). Fitness tests F1/F2 guard both against drift. | AC1, AC2 |
| `ViewModels/Pages/AppConfigurationPageViewModel.cs` | Inject `IEventPublisher`. Add detection and recovery to `Initialize` (2.3). Guard the manager `Set` in the three `On<X>Changed` partials (2.4). `OnNavigatedToAsync` becomes `async` so it can await the publish. | AC1–AC3 |
| `Views/Windows/MainWindow.xaml.cs` | (a) Inject `IEventPublisher` and port the sample's `SubscribeForNotifications` + `ShowNotificationInSnackBar`, called from `InitializeWindow` after `SetSnackbarPresenter`. (b) Guard the theme read: on exception use `Constants.AppConfiguration.Defaults.Theme`. (c) `ClosingHandler`: guard the `ConfirmBeforeExit` read (on exception treat it as `false`, so the window closes without asking), and move the `ConfirmBeforeExit = false` set **inside** the existing try block with the save. | AC1 (delivery), "or the app" |
| `Bootstrap/Steps/SetUILanguageStep.cs` | Guard the `Language` read. On exception, log a warning and skip `SetCulture`, so the step completes. Add an `ILogger<SetUILanguageStep>` constructor parameter (resolved by DI). | "or the app" |
| `Resources/l10n/Localization.resx` + `Localization.pt-BR.resx` | Two new keys (2.6). | AC1 |
| `tests/.../Fakes/FakeAppConfigurationManager.cs` | Add `Seed(string key, object? value)` (stores a value without writing to `CallLog`, so it can seed wrong-typed values), `Exception? ThrowOnTryGet`, and `Exception? ThrowOnSet`. | AC4 |
| `tests/.../Fakes/FakeEventPublisher.cs` | `Register` records `(Type eventType, object recipient, Delegate handler)` instead of throwing `NotSupportedException` (needed now that `MainWindow` registers). `Unregister` removes the record. | AC1 |
| `tests/.../Views/Windows/MainWindowTests.cs` | Pass a `FakeEventPublisher` in `CreateWindow`. Add the tests in section 8. | AC1 |
| `tests/.../Views/Pages/LocalizationFitnessTests.cs` | Add the two new keys to `Keys`. | AC1 |

No new production types, interfaces or packages.

### 2.3 Detection and recovery in `Initialize` (binding for the developer)

`Initialize` stays guarded by `_initialized` and runs once per VM instance. The VM is transient, so it runs once per navigation to the page.

**Step 1: read and validate.** Wrap the **whole** read in one `try`. Any exception from the manager means **unreadable**, so go to recovery. Then check each setting `(key, T, isSupported)`:

| Setting | Key | `T` | Supported when |
|---|---|---|---|
| Theme | `Keys.Theme` | `Theme` | `theme.Find(AvailableThemes)` is not `EmptyComboBoxItem` (equivalent to `Enum.IsDefined`) |
| Language | `Keys.Language` | `string` | `CultureResolver.TryGet(value, out c)` **and** `c.Find(AvailableLanguages)` is not `EmptyComboBoxItem` |
| Confirm before exit | `Keys.ConfirmBeforeExit` | `bool` | always |

For each setting:
1. `present = manager.TryGet<object>(key, out _)`.
2. If **not present**, use `Defaults.X`. This counts as **valid** (first run, or `{}`). This rule is what keeps AC3 free of false positives.
3. If present and `!manager.TryGet<T>(key, out v)`, the setting is **invalid** (wrong type).
4. If present and `v` is not supported, the setting is **invalid** (unsupported value).

Note: this **replaces** IPX-0004's silent `CurrentUICulture` fallback for the language. An unsupported stored language is now invalid and gets recovered, instead of showing a blank combo.

The `TryGet<object>` probe works against both the real manager (`JsonElement.Deserialize<object>()` returns a boxed `JsonElement` for any non-null JSON value) and the fake (`stored is object`). A JSON `null` reads as "absent", which is acceptable.

**Step 2a, all valid (happy path, AC3):** assign `CurrentTheme`, `CurrentLanguage` and `CurrentConfirmBeforeExit` from the validated values, then set `_initialized = true`. Make **no** `Set`, no `Save`, no publish, no `IApplicationThemeApplier.Apply` and no `T.SetCulture`. This is the same as IPX-0004 today.

**Step 2b, any invalid or unreadable: recover** (AC1, AC2). **All** settings reset, not only the bad one, as AC2 requires. Steps, in this order:
1. **Log** one warning: `"Application configuration is invalid or unreadable; resetting to defaults."` with the exception (unreadable) or the offending **key names and reason** (`WrongType` / `Unsupported`). **Do not log raw stored values** (2.7).
2. **Reset in memory:** in one `try`, set `Theme`, `Language` and `ConfirmBeforeExit` to the `Defaults`. If all three `Set` calls succeed, call `QueueSave()` **once**. If any `Set` throws (a persistently unreadable file), log it, skip the save and carry on. The page still shows the defaults.
3. **Apply live** so the UI matches the reset values: `_applicationThemeApplier.Apply(Defaults.Theme)` and `T.SetCulture(Defaults.Language)`. These are the same calls the change handlers make.
4. **Assign** `CurrentTheme`, `CurrentLanguage` and `CurrentConfirmBeforeExit` from the defaults. They are found in `AvailableThemes`/`AvailableLanguages`, so they are **never** `EmptyComboBoxItem`. Then set `_initialized = true`. The flag is still set **after** the assignments, so no `On<X>Changed` partial queues a second save.
5. **Notify:** back in `OnNavigatedToAsync`, after `Initialize` returns `recovered == true`, `await _eventPublisher.PublishFailureAsync<SnackBarNotificationEvent>(title: T[TitleKey], message: T[MessageKey])` **exactly once**. Publish after `SetCulture`, so the text is in the reset language. Wrap the publish in a try block that logs: a failing notification must not break navigation.

`OnNavigatedToAsync` must never throw because of the manager.

**Shape:** private and up to the developer, for example `private bool Initialize()` returning `recovered`, with a private `TryReadSettings(out ...)` helper. Keep it inside the VM. The "supported" sets are the VM's own `Available*` arrays, so a separate validator type would be premature (CLAUDE.md: minimum abstraction).

### 2.4 Keeping the page usable after recovery (AC2)

When the manager keeps throwing, every `Set` throws. Today the three `On<X>Changed` partials would let that exception escape into the WPF binding.

- In each partial, keep the order **apply live → `Set` → `QueueSave()`**, and wrap **only the `Set`** in a try block. On an exception, log a warning and **skip** `QueueSave()`. The live change (theme or culture) has already happened, and the combo keeps the user's choice.
- `SaveAfterAsync` already never throws (IPX-0004), so no change is needed there.

### 2.5 Snackbar subscription in `MainWindow` (AC1 delivery)

This is a port of the sample with no other changes:

```csharp
// in InitializeWindow, after _snackBarService.SetSnackbarPresenter(SnackBarPresenterRoot):
_eventPublisher.Register<SnackBarNotificationEvent>(this, ShowNotificationInSnackBar);

private void ShowNotificationInSnackBar(object sender, SnackBarNotificationEvent evt) {
    Dispatcher.InvokeAsync(() => _snackBarService.Show(evt));   // Nameless.Windows.Notification.SnackBar extension
}
```

`MainWindow` is resolved before the splash and lives for the whole process, so the subscription exists before any page can publish. No `Unregister` is needed. This also makes IPX-0004's "missing log file" warning visible for the first time. That is a behaviour change QA should see, and it is intended.

### 2.6 Notification text

| Key | Neutral (English) | `pt-BR` |
|---|---|---|
| `AppConfigurationPage_InvalidSettings_Title` | `Settings reset` | `Configurações redefinidas` |
| `AppConfigurationPage_InvalidSettings_Message` | `Your saved settings could not be read and were reset to the defaults.` | `Não foi possível ler as configurações salvas. Elas foram redefinidas para os valores padrão.` |

The type is `NotificationType.Failure` (danger appearance), because the ticket asks for an "error notification". The timeout is the extension's fixed 5 s. There is no action button.

### 2.7 Assumptions

- **A1:** the snackbar and the reset fire when the **configuration page loads**, not at app startup (2.1). Startup readers only avoid crashing. A user who never opens the settings page runs on per-read defaults and is not notified.
- **A2:** "all settings reset to defaults" means the three settings this app stores (Theme, UILanguage, ConfirmBeforeExit), to the `Nameless.Windows` extension defaults. Valid settings are reset too when any one is bad, as AC2 states literally.
- **A3:** an **unsupported but well-formed** language (`"fr-FR"`) or theme (`42`) counts as invalid. Treating it as valid would leave the blank combo that AC2 forbids.
- **A4:** if the save after recovery fails (the file is still locked), the next navigation detects the problem again and shows the snackbar again. That is truthful, and acceptable.

## 3. Contract

There is no HTTP API or event schema. The in-process contracts are:

```csharp
// ViewModels/Pages: constructor gains one parameter (DI-resolved; the VM is not a public library type)
public AppConfigurationPageViewModel(
    IAppConfigurationManager appConfigurationManager,
    IApplicationContext applicationContext,
    IApplicationThemeApplier applicationThemeApplier,
    ITranslator translator,
    IMediator mediator,
    IEventPublisher eventPublisher,          // new
    ILogger<AppConfigurationPageViewModel> logger)

// Views/Windows: MainWindow constructor gains IEventPublisher (DI-resolved)
// Bootstrap/Steps: SetUILanguageStep constructor gains ILogger<SetUILanguageStep> (DI-resolved)
```

Behavioural invariants:
- **I1:** if the settings are valid or absent, `OnNavigatedToAsync` makes zero `Set`, zero `Save`, zero published events, zero theme applications and zero `SetCulture` calls.
- **I2:** if any setting is invalid or unreadable, `OnNavigatedToAsync` publishes exactly one `SnackBarNotificationEvent` of type `Failure`. It sets all three defaults and queues exactly one save (zero saves if `Set` throws), and the VM ends up with non-empty `CurrentTheme`/`CurrentLanguage` equal to the defaults.
- **I3:** `OnNavigatedToAsync`, the `On<X>Changed` partials, `MainWindow` construction, `MainWindow.ClosingHandler` and `SetUILanguageStep.ExecuteAsync` never throw because of `IAppConfigurationManager`.
- The persisted keys and format are unchanged. The change is additive (constructor additions only, all resolved by DI) and **not breaking**.

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function (the developer implements it) |
|---|---|---|
| Robustness / availability (no crash) | One guarded read in the VM, guarded `Set` in the change handlers, and guarded reads in the three startup/close readers. | Tests R5, M1–M2 and S1 (section 8), each with a fake that throws on every call. |
| Correctness (no false positives) | The "absent means default and valid" rule, and presence probing only on keys that exist. | Test H1 covers an empty manager, all-valid values and `pt_BR` normalisation. |
| Contract drift with `Nameless.Windows` | Key names and defaults are duplicated in `Constants`, because the package keeps them private. | **F1 (key names):** for each setting, set it through the extension property on a fresh fake, then assert `fake.TryGet<object>(Constants.AppConfiguration.Keys.X, out _)` is `true`. **F2 (defaults):** an empty fake's `Theme`/`Language`/`ConfirmBeforeExit` getters equal `Constants.AppConfiguration.Defaults.*`. Both fail at the next package bump if `Nameless.Windows` renames a key or changes a default. |
| Localisability | Two new keys in both resx files. | The existing `LocalizationFitnessTests.Keys` list gains the two keys. |

## 5. Security notes (light threat model)

- **Entry point:** the "untrusted" input is `<FileManager.Root>/app.config`. It is writable only by the same local user, so tampering gives an attacker nothing they did not already have. The real risk is **availability** (crash or unusable UI), which the design addresses.
- **Validation:** only values from the fixed `AvailableThemes`/`AvailableLanguages` sets are applied. An arbitrary culture string is never passed to `T.SetCulture` from the page. (`SetUILanguageStep` still passes anything `CultureResolver` accepts, which is unchanged and harmless.)
- **Logging:** log **key names and reason categories, never raw stored values.** The file is user-editable, so a raw value could be arbitrary text, including newlines that would forge log lines. Exceptions are logged as exception objects, the way the manager already logs them.
- **No new data:** no PII and no secrets are stored. Do not add anything sensitive to `IAppConfigurationManager`, as stated in the IPX-0004 rule.
- Nothing else applies: there is no network, no authN/authZ, and a single local user.

## 6. Risks

1. **D-1 limitation:** malformed JSON is invisible, which is the most literal "corrupted settings file" case. The app does not crash and the page is usable with defaults, but **no snackbar appears**. See section 9.
2. **Duplicated key names and defaults:** if `Nameless.Windows` changes a key, the presence probe reports "absent" and the setting counts as valid. This fails safe (no false snackbar), and F1/F2 catch it.
3. **The snackbar subscription is new behaviour app-wide:** IPX-0004's log-file warning becomes visible. QA should expect it.
4. **`T.SetCulture("pt-BR")` during recovery** changes the whole UI language. That is intended (AC2: reset to defaults), but a user who ran in English with one corrupted key will find the UI switched to Portuguese. That is the consequence of A2.
5. **The theme applier is called during navigation.** WPF-UI's `ApplySystemTheme` is already exercised by IPX-0004's change handler, so no new risk. Tests use `FakeApplicationThemeApplier`, as IPX-0004 does.
6. **`OnNavigatedToAsync` becomes `async`.** The existing tests call it without awaiting. With synchronous fakes it still completes inline. Keep the publish as the **last** statement and do not add `ConfigureAwait(false)` (the IPX-0004 save chain must start on the UI thread).

## 7. Work split and tasks (backend-developer-dotnet), in build order

1. `Constants.AppConfiguration.Keys` / `.Defaults`, with fitness tests F1 and F2. (AC1, AC2)
2. Resx: the two keys in `Localization.resx` and `Localization.pt-BR.resx`, plus the `LocalizationFitnessTests.Keys` entries. (AC1)
3. Test fakes: add `Seed`, `ThrowOnTryGet` and `ThrowOnSet` to `FakeAppConfigurationManager`; make `FakeEventPublisher.Register`/`Unregister` record. (AC4)
4. `MainWindow`: inject `IEventPublisher` and add the snackbar subscription (2.5), the guarded theme read and the guarded closing handler. Update `MainWindowTests.CreateWindow` and add tests M1–M3. (AC1 delivery, no-crash)
5. `SetUILanguageStep`: guarded read and logger, plus test S1. (no-crash)
6. `AppConfigurationPageViewModel`: inject `IEventPublisher`, add detection and recovery (2.3) and the guarded `Set` (2.4). Update the `CreateViewModel` factories in both VM test files and add tests R1–R6 and H1–H2, writing the tests first. (AC1–AC4)
7. Manual QA checklist (section 8), against the real manager.

Frontend tasks: none (no separate frontend stack).

## 8. Test strategy

The tests use xUnit with hand-rolled fakes (no mocking library), on STA through `StaWindowTestHarness` wherever `ComboBoxItem`/WPF types are created, in the `WpfUiThemeTestCollection`, the same as the existing VM tests. Every AC test uses `FakeAppConfigurationManager` (AC4).

| AC | Test | Level |
|---|---|---|
| AC1 | **R1 (unreadable):** set `ThrowOnTryGet = new IOException()`, then navigate. Exactly one published `SnackBarNotificationEvent`, `Type == Failure`, title and message equal the translator output for the two keys, and no exception escapes. | Unit (VM) |
| AC1 | **R2 (invalid data, Theory):** `Seed` one bad value per row: `("Theme", "Dark")` wrong type, `("Theme", (Theme)42)` undefined, `("UILanguage", 5)` wrong type, `("UILanguage", "xx-INVALID")`, `("UILanguage", "fr-FR")` (valid culture, unsupported), `("ConfirmBeforeExit", "yes")`. Each row produces exactly one Failure snackbar. | Unit (VM) |
| AC2 | **R3 (reset):** after R2's `("Theme", (Theme)42)` with the other keys seeded to valid non-defaults (`en-GB`, `true`), the manager holds `Theme.System`, `"pt-BR"` and `false`. `CallLog` contains the three `Set:` entries followed by exactly one `Save`. `CurrentTheme.Tag == Theme.System` and `CurrentLanguage.Tag == "pt-BR"`, and neither is `EmptyComboBoxItem`. `FakeApplicationThemeApplier` received `Theme.System`, and `FakeTranslator.SetCultureCalls == ["pt-BR"]`. | Unit (VM) |
| AC2 | **R4 (usable afterwards):** after R3, set `CurrentTheme` to Dark. The manager holds Dark, a second `Save` is logged, and nothing throws. | Unit (VM) |
| AC2 | **R5 (persistently unreadable):** `ThrowOnTryGet` and `ThrowOnSet` both set. Navigating does not throw and shows one snackbar. The VM shows the defaults (not Empty), and there are **zero** `Save` calls. Then changing `CurrentTheme` does not throw, the applier is called, and there are still zero saves. | Unit (VM) |
| AC2 | **R6 (once per instance):** a second `OnNavigatedToAsync` on the same recovered VM publishes nothing more. | Unit (VM) |
| AC3 | **H1 (Theory):** (a) an empty fake (first run), (b) seeded valid `Dark` / `"en-GB"` / `true`, (c) `"pt_BR"` (normalised by `CultureResolver`, so supported). Each produces zero published events, an empty `CallLog`, no applier calls and no `SetCulture` calls. | Unit (VM) |
| AC3 | **H2:** the existing IPX-0004 VM tests stay green unchanged, apart from the factory parameter. | Unit (VM) |
| AC1 delivery | **M1:** `MainWindow` construction registers exactly one handler for `SnackBarNotificationEvent` on the `FakeEventPublisher`. | Unit (STA) |
| no-crash | **M2:** `MainWindow` with `ThrowOnTryGet` set constructs without throwing, and the applier receives `Theme.System`. **M3:** `ClosingHandler` with a throwing read does not cancel the close and shows no dialog. | Unit (STA) |
| no-crash | **S1:** `SetUILanguageStep.ExecuteAsync` with a throwing manager completes and makes no `SetCulture` call. | Unit |
| drift | **F1, F2** (section 4). | Unit |
| (optional) | **I1:** the real `AppConfigurationManager` over a temp-dir `IFileManager` (the IPX-0004 integration-test pattern) with `app.config` = `{"Theme":"Dark"}`. The VM publishes a snackbar, and after the save the file reads back `Theme` = `0`. This proves the `TryGet<object>` probe against the real implementation. It is **in addition to** the fake-based AC tests, not a replacement. | Integration |

**Manual QA against the real manager** (edit `<app data>/app.config` with the app closed, launch, then open Configurações):

| Case | Expected |
|---|---|
| `{"Theme":"Dark"}` | Snackbar shown and settings reset. Close and relaunch: no snackbar. |
| `{"Theme":42}` and `{"UILanguage":"fr-FR"}` | Snackbar shown, and the combos are not blank. |
| `{"ConfirmBeforeExit":"yes"}` | Snackbar shown. Closing the app does not ask. |
| Malformed JSON (`{{{`) | **No snackbar** (D-1 limitation). Defaults are used and nothing crashes. |
| File locked exclusively before launch (PowerShell `[IO.File]::Open($p,'Open','ReadWrite','None')`, kept open) | The app starts with the system theme and does not crash. The settings page shows the snackbar and the defaults. Changes apply live but are not saved (logged). Closing the app does not crash. |
| Valid file, or no file | No snackbar, ever. |
| Open log file when the logs folder is missing | The warning snackbar is now actually visible (2.5). |

**Risky failure modes for QA to sharpen:** a false-positive snackbar on a normal launch (H1 must cover `{}`); a recovery snackbar shown twice on one navigation; blank combos after recovery; a crash on close while the file is locked; the language switching to pt-BR after recovery (expected, see risk 4).

## 9. Open decisions

**D-1 (NEEDS-DECISION, not blocking): malformed `app.config` cannot be detected through `IAppConfigurationManager`.**
The real manager swallows JSON parse errors and returns an empty dictionary, which is indistinguishable from a first run. AC1's "unreadable" is therefore met for files that cannot be opened and for bad values, but **not** for a syntactically corrupt file. In that case the app silently uses the defaults: it does not crash and the page is usable, but no snackbar appears.

| Option | Cost | AC1 for a malformed file |
|---|---|---|
| **A (recommended):** ship this plan as is. File a `Nameless.Windows` issue to expose load failure (for example a `LoadFailed`/`LastLoadError` member on `IAppConfigurationManager`, or rethrowing an `AppConfigurationLoadException` from `Reload()`), plus an InfoPhoenix follow-up ticket to consume it. Option A also lets the startup readers report it. | None now. A small follow-up once the package releases. | Not met until the follow-up. |
| B: change `Nameless.Windows` first, release it, bump `Directory.Packages.props` in this ticket and consume the new member. | A cross-repo change and release. This ticket is blocked on it and gains a dependency bump. | Met. |
| C: InfoPhoenix opens `<FileManager.Root>/app.config` itself and tries to parse it. | It contradicts the ticket's out-of-scope ("on-disk format ... belongs to `Nameless.Windows`"), duplicates the package's private file name, and is fragile. | Met, but fragile. |

**Recommendation: A.** The tasks in section 7 are identical under A and B. B only adds one detection branch later.

**Assumptions for the ticket owner to confirm (not blocking):** A1 (recovery fires on page load, not at startup) and A2 (all three settings reset, including the UI language switching to pt-BR).

**Upstream notes (not this repo):** `AppConfigurationManager.GetAppConfiguration` opens the file outside its try block, so a locked or denied file throws from every `TryGet`/`Set` instead of degrading like a parse error does. Worth including in the same `Nameless.Windows` issue as D-1.

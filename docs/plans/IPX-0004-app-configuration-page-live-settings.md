# IPX-0004: Application configuration page with live settings

- Ticket: [IPX-0004-AppConfigurationPageLiveSettings (#13)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/13)
- Epic: [#11 Setup & Initialization](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11) (Part 2, decisions D6–D10)
- Stack: .NET 10 / WPF (`dotnet`), single project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. There is no separate frontend; the WPF UI is part of the same .NET project.
- UX spec: **not needed**. This is a port of the sample's established settings page inside the existing WPF-UI chrome, and CLAUDE.md skips `ux-specialist` for this project.
- ADR: **none**. Saving immediately instead of at shutdown is a call-site choice. It needs no framework change, no external integration, and no schema or data-model change (`app.config` keeps the same keys and format, written by the same `Nameless.Windows` code). Section 2.4 records the decision.

## 1. Context

Ground truth as of 2026-11-05: the sample was read on GitHub `main`, and `Nameless.Windows` / `Nameless.Common` 4.0.0 were checked against the installed package XML docs and the matching sources.

**InfoPhoenix today (this worktree):**
- `MainWindow` already hosts a `ui:NavigationView` whose menu and footer items come from `INavigationViewItemProvider` (auto-discovered `[NavigationViewItem]` pages). The nav shell exists, but no page exists yet. There is no `Views/Pages/`, `ViewModels/Pages/` or `Features/`.
- `MainWindow.InitializeWindow` hard-codes `ApplicationThemeManager.Apply(ApplicationTheme.Light)`.
- `ApplicationHostFeature` does **not** configure the translator. `TranslatorHostFeature` therefore registers whatever its options resolve to; the sample explicitly calls `WithTranslator<ResourceTranslator>()`. `appsettings.json` already has `Translator:ResourceFullName`, but `Localization.resx` is still the empty template, with no keys and no `pt-BR` satellite.
- The Serilog file sink path is relative (`logs/app-.log`), so it resolves against the process **working directory**.
- `App.OnCurrentHostStop` does nothing.

**Package facts this design depends on:**

| Fact | Source |
|---|---|
| `IAppConfigurationManager` (`Nameless.Windows.AppConfiguration`): `TryGet<T>`, `Set<T>`, `SaveChangesAsync(ct)`, `Reload()`. `Theme`, `Language` and `ConfirmBeforeExit` are C# 14 extension properties over `TryGet`/`Set` (keys `Theme`, `UILanguage`, `ConfirmBeforeExit`; defaults `Theme.System`, `"pt-BR"`, `false`). | `AppConfigurationManagerExtensions` |
| Registered as a **singleton** by `AppConfigurationHostFeature`, which `WinHostFactory.CreateDefault` includes. | `AppHostFeatureCollectionExtensions.CreateDefault` |
| `AppConfigurationManager` backs onto `<FileManager.Root>/app.config` (JSON) through an in-memory `Dictionary<string, JsonElement>` that is **not thread-safe**. | `AppConfigurationManager` |
| `SaveChangesAsync` serialises the dictionary **synchronously**, then writes `app.config.tmp` opened with `FileShare.None` and renames it over `app.config`. **Every exception is caught and logged, never thrown.** A second save that starts while the first still holds the temp file fails to open it, logs, and returns, so its data is not written. | `AppConfigurationManager.SaveChangesAsync` |
| `ProcessHelper.OpenDirectory(path)` / `OpenFile(path)` run `explorer.exe <path>` with the path passed **unquoted** as the argument string. | `Nameless.Windows.Helpers.ProcessHelper` |
| ViewModels deriving from `Nameless.Windows.Mvvm.ViewModel` are registered **transient** by `MvvmHostFeature`, through an assembly scan. | `MvvmHostFeature` |
| `IMediator` request handlers are found through an assembly scan by `MediatorHostFeature` (default feature). | `MediatorHostFeature` |
| `ResourceTranslator.SetCulture` sets `CultureInfo.CurrentCulture`/`CurrentUICulture` on the calling thread and raises `PropertyChanged("Item[]")`, so `{loc:Translator Key}` bindings refresh live. | `ResourceTranslator` |

**Sample (`Nameless.WinApp`) being ported:** `Views/Pages/AppConfigurationPage.xaml(.cs)`, `ViewModels/Pages/AppConfigurationPageViewModel.cs`, `Features/OpenCurrentLogFile/*`, `Bootstrap/Steps/SetUILanguageStep.cs`, the theme and closing handling in `Views/Windows/MainWindow.xaml.cs`, and the `OnCurrentHostStop` save in `App.xaml.cs`.

## 2. Approach

### 2.1 Files

All paths are under `src/Nameless.InfoPhoenix.Client/`.

| Path | Change | AC |
|---|---|---|
| `Views/Pages/AppConfigurationPage.xaml` | New. A port of the sample **without** the Version card's check-for-update button and **without** the Backup and Restore cards (IPX-0005). Cards: Version, Language, Theme, Confirm before exit, App data directory, Log file. | AC1–AC4 |
| `Views/Pages/AppConfigurationPage.xaml.cs` | New. `[NavigationViewItem(Icon = SymbolRegular.Settings48, Title = Constants.Labels.NavigationMenuItems.Configuration, Placement = Placement.Bottom)]`, `INavigableView<AppConfigurationPageViewModel>`, the same as the sample. | AC1 |
| `ViewModels/Pages/AppConfigurationPageViewModel.cs` | New. See 2.3. | AC2–AC4 |
| `Features/OpenCurrentLogFile/OpenCurrentLogFileRequest.cs` | New. `public class OpenCurrentLogFileRequest : IRequest;` (sample). | AC3 |
| `Features/OpenCurrentLogFile/OpenCurrentLogFileRequestHandler.cs` | New. A port of the sample with deviations D3–D5. | AC3 |
| `Bootstrap/Steps/SetUILanguageStep.cs` | New. A port of the sample (applies the stored language at startup). | AC2 (persisted value takes effect) |
| `Constants.cs` | Add `Labels.NavigationMenuItems.Configuration = "Configurações"`. Add `Logging.DirectoryPath` (absolute, see D4) or an equivalent single source. | AC1, AC3 |
| `Infrastructure/ApplicationHostFeature.cs` | (a) Add `ConfigureTranslatorHostFeature`: `features.Get<TranslatorHostFeature>().Configure(o => o.WithTranslator<ResourceTranslator>())`, the same as the sample. (b) Root the Serilog file sink at `AppContext.BaseDirectory` (D4). | AC2, AC3 |
| `Resources/l10n/Localization.resx` | Replace the template sample entries with the page's keys (neutral = **English**, which serves `en-GB`). | AC2 |
| `Resources/l10n/Localization.pt-BR.resx` | New. The same keys in Portuguese; the sample's `TranslationResources.pt-BR.resx` has the texts. No designer generator; an SDK `EmbeddedResource` builds the satellite. | AC2 |
| `Views/Windows/MainWindow.xaml.cs` | (a) Apply the **stored** theme at init instead of the hard-coded `Light` (sample `SetApplicationTheme`). (b) Port the sample's `Closing` handler that honours `ConfirmBeforeExit` (needs `IAppConfigurationManager`, `IMessageDialog` and `ITranslator` injected). | AC2 (persisted values take effect) |
| `App.xaml.cs` | `OnCurrentHostStop`: `await provider.GetRequiredService<IAppConfigurationManager>().SaveChangesAsync(cancellationToken)`, the same as the sample. It is a **safety net only** (2.4). | AC2 |

The `Views/Pages` ↔ `ViewModels/Pages` mirror follows the CLAUDE.md rule. `Features/` holds the one use case that has logic of its own, the log-file lookup. The other actions are one-liners in the VM, as in the sample.

**Scope note (assumption A1).** The ticket's literal scope is the page. Items 7–9 in the table (the language step, the stored theme at startup, the closing handler) and the shutdown save are included because without them the persisted settings have **no effect**. A stored theme or language would be ignored on the next launch, and Confirm-before-exit would be a checkbox with no consumer. Each is a direct port of the sample. If the ticket owner wants them split out, they are the last tasks in the build order (7) and can be removed without touching the page.

### 2.2 Page (binding for the developer)

- Copy the sample XAML card structure, `x:Name="AppConfigurationPageRoot"`, and bindings through `ElementName=AppConfigurationPageRoot, Path=ViewModel.X`.
- **Version card:** header title plus a `TextBlock` with `x:Name="AppVersionTextBlock"` and `Text="{Binding ... ViewModel.AppVersion, Mode=OneTime}"`. **Remove** the check-for-update `ui:Button` (IPX-0005). Use a `TextBlock`, never a `TextBox` (AC4: not editable).
- The Version card icon is `{ui:ImageIcon 'pack://application:,,,/Nameless.InfoPhoenix.Client;component/Resources/imgs/application_64x64.png'}`, component-qualified like `SplashScreenWindow` (IPX-0003 risk 5), **not** the sample's relative `/Resources/...`.
- **Language card:** a `ComboBox`, `ItemsSource=AvailableLanguages` (OneTime), `SelectedValue=CurrentLanguage` (TwoWay).
- **Theme card:** the same pattern with `AvailableThemes` / `CurrentTheme`.
- **Exit card:** `CheckBox IsChecked=CurrentConfirmBeforeExit` (TwoWay).
- **App data directory card:** a button bound to `OpenApplicationDataDirectoryCommand`.
- **Log file card:** a button bound to `OpenApplicationLogFileCommand`.
- Text keys use the sample's names (`AppConfigurationPage_Title`, `AppConfigurationPage_VersionCard_Title`, `AppConfigurationPage_ApplicationLanguageCard_Title/_Message`, `AppConfigurationPage_ThemeCard_Title/_Message`, `AppConfigurationPage_ExitBehaviorCard_Title/_Message`, `AppConfigurationPage_AppDirectoryPathCard_Title/_Message/_Button_Caption`, `AppConfigurationPage_ApplicationLogCard_Title/_Message/_Button_Caption`), plus the closing dialog's `MainWindow_ConfirmApplicationExit_MessageBox_Title/_Message` and the log handler's warning keys (D5). **Every key must exist in both resx files** (fitness function F2).

### 2.3 ViewModel (binding for the developer)

```
public partial class AppConfigurationPageViewModel : ViewModel, INavigationAware
ctor(IAppConfigurationManager, IApplicationContext, ITranslator, IMediator, ILogger<AppConfigurationPageViewModel>)
```

Members: `CurrentTheme`, `CurrentLanguage` (`[ObservableProperty] ComboBoxItem`, default `ComboBoxItemHelper.EmptyComboBoxItem`), `CurrentConfirmBeforeExit` (`[ObservableProperty] bool`), `AppVersion` (`string`, get-only to the view), `AvailableThemes` and `AvailableLanguages` (the sample's exact arrays: System/Light/Dark/HighContrast and `pt-BR` "Português (Brasil)" / `en-GB` "English (United Kingdom)").

- `OnNavigatedToAsync` → `Initialize()` once, guarded by `_initialized`, the same as the sample. It reads `AppVersion` from `IApplicationContext.Version`, and theme, language (`CultureResolver.TryGet`, falling back to `CurrentUICulture`) and confirm-before-exit from the manager. **Initialization must not trigger a save**: the `_initialized` guard handles this, because the flag is set *after* the properties are assigned.
- `On<X>Changed(old, new)` partials, in this order:
  1. Return early if `!_initialized` or the value is unchanged. Compare tags with `Equals(old.Tag, new.Tag)`, not `==`, because the sample's `==` on `object` is a reference comparison that only works by accident.
  2. **Apply live.** Theme: `ApplicationThemeManager.Apply(theme.ToApplicationTheme())`. Language: `T.SetCulture(isoCode)`. Confirm-before-exit has no visual effect; its live consumer is `MainWindow`'s `Closing` handler, which reads the singleton manager on every close.
  3. **Set in memory**: `_appConfigurationManager.Theme / Language / ConfirmBeforeExit = ...`.
  4. **Persist now**: `QueueSave()`.
- `QueueSave()` (the only new mechanism, see 2.4):
  ```
  private Task _pendingSave = Task.CompletedTask;
  private void QueueSave() => _pendingSave = SaveAfterAsync(_pendingSave);
  private async Task SaveAfterAsync(Task previous) {
      await previous;                       // never faults (see catch below)
      try { await _appConfigurationManager.SaveChangesAsync(CancellationToken.None); }
      catch (Exception ex) { /* log via ILogger; never rethrow */ }
  }
  ```
  - **Do not use `ConfigureAwait(false)` / `SkipContextSync()` anywhere in this chain.** `SaveChangesAsync` serialises the non-thread-safe dictionary synchronously at its start. It must start on the UI thread, which is the same thread that calls `Set`.
  - Do not add a debounce or coalescing. Saves are tiny and user-paced, and the chain already guarantees the last save writes the latest state.
  - If `catch (Exception)` triggers an analyzer rule under `TreatWarningsAsErrors`, narrow it to `IOException`/`UnauthorizedAccessException` rather than suppressing. In practice `SaveChangesAsync` already swallows everything.
- Commands:
  - `OpenApplicationDataDirectoryCommand` → `ProcessHelper.OpenDirectory(Quote(_applicationContext.FileManager.Root))` (D3).
  - `OpenApplicationLogFileCommand` → `await _mediator.ExecuteAsync(new OpenCurrentLogFileRequest(), ct)` (sample).

### 2.4 Save-timing decision (the ticket's divergence from the sample)

- **Decided by the ticket:** each change persists immediately. The sample saves only in `OnCurrentHostStop`.
- **Race found:** two changes in quick succession, for example a fast double-toggle of the checkbox or switching language while a theme save is still in flight, would make the second `SaveChangesAsync` fail on `app.config.tmp` (`FileShare.None`). The failure is only logged, so the newer value is silently not written. **Fix:** the per-VM save chain in 2.3 runs saves strictly one after another. Each save serialises the full current dictionary when it starts, so the last one always writes the latest state.
- **Shutdown save kept as a safety net (D1).** It covers in-memory changes made outside the page, namely the closing handler's "No" (= stop asking), and any immediate save that failed. It is best-effort: `App.OnExit` is `async void`, so the process may end before an awaited save completes. That limitation is inherited from the sample, and it is exactly why immediate saving is the primary path.
- **Residual edge (accepted):** the shutdown save is not part of the VM's chain. If an immediate save is still in flight at the moment of exit, one of the two can fail to open the temp file. Either way the file holds a complete state (write-then-rename), because the manager never writes a partial file. The worst case is losing the closing handler's "stop asking" flag for one run. A shared, process-wide save gate would close this edge, but it adds a new infrastructure type for a millisecond window, so it is not worth it now.
- **Performance:** one roughly 100-byte JSON write plus a rename per user click. It is negligible and happens off the UI thread after the synchronous serialise.
- **Silent save failure:** `SaveChangesAsync` never reports failure (it only logs). The user is not told when a save fails, for example on a read-only or full disk. Surfacing that is error-UX and belongs with IPX-0006 (invalid-state recovery), not here. The plan does not wrap the manager to detect failures.

### 2.5 Deviations from the sample (each decided here and revertible)

| # | Sample | InfoPhoenix | Why |
|---|---|---|---|
| D1 | Save only in `OnCurrentHostStop` | Immediate save per change (serialised chain) **plus** the same shutdown save | Ticket AC2, and 2.4. |
| D2 | Version card has a check-for-update button; Backup and Restore cards | Omitted | Out of scope (IPX-0005 adds them stubbed). |
| D3 | `ProcessHelper.OpenDirectory(path)` with an unquoted path | Pass `"\"" + path + "\""` | `ProcessHelper` passes the path as a raw argument string. Paths with spaces (`C:\Program Files\...`, `C:\Users\John Smith\...`) make `explorer.exe` open the wrong location (usually *Documents*). Quote at the call site and report the bug upstream to `Nameless.Windows`; do not fix the package here. |
| D4 | Serilog sink `logs/app-.log` relative to the working directory; the handler looks in `AppContext.BaseDirectory/logs` | Both use **`SysPath.Combine(AppContext.BaseDirectory, Constants.Logging.Directory)`**, the sink path built from the same constant | When the app is started with a different working directory (a shortcut's "Start in", a terminal), the sink writes elsewhere and the button finds nothing. One source of truth fixes AC3 for every launch path. |
| D5 | Handler takes `OrderBy(f => f.LastWriteTime).FirstOrDefault()` | `OrderByDescending(...)` | The sample opens the **oldest** of the retained files (up to 3), but AC3 wants the current one. The handler must also treat a **missing `logs` directory** as "no log file" (warning snackbar), not throw. Snackbar keys: `OpenCurrentLogFileRequestHandler_Missing_Log_File_Title/_Message`, or the sample's names; either is fine, but they must be in both resx files. The current file is opened with `ProcessHelper.OpenFile(Quote(path))` (default viewer), per the AC's "default viewer". |
| D6 | `oldValue.Tag == newValue.Tag` | `Equals(oldValue.Tag, newValue.Tag)` | Correctness: the original compares `object` references. |

`OnCurrentHostStop`: use a plain `await` (no `SkipContextSync` needed there, but harmless if the sample's extension is available). The developer's choice.

### 2.6 Known limitations (accepted, the same as the sample)

- The nav item title comes from a `const` in an attribute, so it **cannot be localised at runtime**. It stays "Configurações" in both languages. Fixing that needs a `Nameless.Windows` change.
- Theme combo item texts (`Theme.ToComboBoxItem()`) are created once with the culture active at VM construction. They refresh on the next page instance, not live.
- `SystemThemeWatcher.Watch(this)` stays (sample). It may re-apply the system theme when Windows switches light/dark, even when the user chose an explicit theme. This is sample behaviour and out of scope; QA should note it, not fail on it.
- `AppVersion` shows `AssemblyInformationalVersion` as-is. With SourceLink this may include a `+<commit-sha>` suffix, consistent with the splash.

## 3. Contract

There is no HTTP API or event. The in-process contracts are:

```csharp
// ViewModels/Pages
public partial class AppConfigurationPageViewModel : ViewModel, INavigationAware {
    public ComboBoxItem CurrentTheme { get; set; }          // Tag: Nameless.Windows.UI.Theme
    public ComboBoxItem CurrentLanguage { get; set; }       // Tag: string ISO code ("pt-BR" | "en-GB")
    public bool CurrentConfirmBeforeExit { get; set; }
    public string AppVersion { get; }                       // read-only to callers
    public ComboBoxItem[] AvailableThemes { get; }
    public ComboBoxItem[] AvailableLanguages { get; }
    public IAsyncRelayCommand OpenApplicationDataDirectoryCommand { get; }
    public IAsyncRelayCommand OpenApplicationLogFileCommand { get; }
}

// Features/OpenCurrentLogFile
public class OpenCurrentLogFileRequest : IRequest;
```

- Invariant: after `On<X>Changed` returns, the manager holds the new value **and** `SaveChangesAsync` has been called (or queued behind an in-flight save). Initialization never calls `SaveChangesAsync`.
- Persisted keys and format: unchanged and owned by `Nameless.Windows` (`Theme`, `UILanguage`, `ConfirmBeforeExit` in `<FileManager.Root>/app.config`).
- The `AppVersionTextBlock` element name is part of the test contract.
- The change is additive and not breaking.

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function (developer implements) |
|---|---|---|
| Durability / consistency of settings | Immediate save, serialised chain, the manager's atomic write-then-rename, and the shutdown safety net. | **F1:** a VM test with a fake manager whose `SaveChangesAsync` returns a pending `TaskCompletionSource` task. Make two changes and assert only **one** save started. Complete it and assert the second started, and that it ran after the second `Set`. |
| Responsiveness | The save is async and the UI thread only does the tiny synchronous serialise. Theme and culture apply synchronously, as in the sample. | None needed beyond code review. |
| Localisability | Neutral (English) and `pt-BR` resx files with the same keys. | **F2:** a test that extracts every `{loc:Translator <Key>}` from `AppConfigurationPage.xaml` (read as a linked or embedded file, or a hard-coded key list kept next to the test) plus the handler and dialog keys. For each key it asserts `ResourceManager.GetString(key, culture)` is non-empty for `InvariantCulture` and `pt-BR`, and that the `pt-BR` value differs from the neutral one. This stops a missing translation from showing up as `[Key]` at runtime. |
| Operability | Log sink and log lookup share one absolute path (D4). | Covered by F2-style review plus the manual QA in 7. |

## 5. Security notes (light threat model)

- **New entry points:** two buttons that start `explorer.exe`. Their arguments come only from `IApplicationContext.FileManager.Root` and the app's own `logs` folder, **never** from user input or `app.config`. There is no injection path, and the quoting (D3) is for correctness, not sanitising. Rule: keep it that way, and never build the argument from a stored setting.
- **Stored data:** `app.config` holds only `Theme`, `UILanguage` and `ConfirmBeforeExit`. There is no PII and there are no secrets. Rule: do not add anything sensitive to `IAppConfigurationManager`.
- **Tampered `app.config`:** an attacker with write access to the user's app data folder already has the user's rights. An invalid `UILanguage` is handled by `CultureResolver` (falls back to `CurrentUICulture`), and an unknown theme falls back to the enum default. Deeper invalid-state handling is IPX-0006.
- **Logs:** the save-failure log line must not include the dictionary contents (it does not today; the manager logs `ex.Message`).
- Nothing else applies: no network, no authN/authZ, single local user.

## 6. Risks

1. **`ApplicationThemeManager.Apply` in the test host.** WPF-UI may need `Application.Current`. If the theme-change VM test throws without one, introduce a one-method seam (`IApplicationThemeApplier` in `Infrastructure/`, registered as a singleton, with the default calling `ApplicationThemeManager.Apply`) and report it as a deviation. Check this first, in task 3.
2. **Translator registration.** Without `WithTranslator<ResourceTranslator>()` the app keeps whatever `TranslatorHostFeature` resolves by default (possibly `NullTranslator`), and the language change becomes a silent no-op. Confirm at runtime that `ITranslator` resolves to `ResourceTranslator`.
3. **Step discovery.** The sample registers no steps explicitly, so `SetUILanguageStep` is expected to be found by `BootstrapperHostFeature`'s assembly scan. If it is not, register it with `features.Get<BootstrapperHostFeature>().Configure(o => o.WithStep<SetUILanguageStep>())` and report it. This step also makes the IPX-0003 splash show a real step for the first time. Check that the splash still closes.
4. **Page registration.** The sample registers no pages explicitly either. Confirm that clicking the nav item resolves the page and VM from DI (`NavigationViewPageProvider`). If it does not, register them as transient in `ApplicationHostFeature` and report it.
5. **Transient VM.** A new VM per navigation means a new save chain per instance. Only one page instance is alive at a time, so this is fine. If WPF-UI caches pages, it is also fine.
6. **D4 moves the log location** when the working directory was not the exe folder. Old logs left in a stray working directory are not migrated, which is acceptable.
7. **Shutdown race** (2.4 residual edge): accepted and documented.

## 7. Work split and tasks (backend-developer-dotnet), in build order

1. `Constants.cs` labels and log path. `ApplicationHostFeature`: translator registration plus the Serilog sink rooted at `AppContext.BaseDirectory` (D4). (AC2, AC3)
2. Resx: neutral English keys and the new `Localization.pt-BR.resx`. Remove the template's sample entries (`Name1`, `Color1`, `Bitmap1`, `Icon1`) from the neutral file. (AC2)
3. `ViewModels/Pages/AppConfigurationPageViewModel.cs` with `Initialize`, the three change handlers, `QueueSave` and the two commands (D3, D6). Write its tests first (T2–T5, F1) and settle risk 1 here. (AC2, AC4)
4. `Features/OpenCurrentLogFile/` request and handler (D3–D5), plus test T6. (AC3)
5. `Views/Pages/AppConfigurationPage.xaml(.cs)` with the nav attribute, plus tests T1 and F2. Confirm risk 4 at runtime. (AC1, AC4)
6. `App.OnCurrentHostStop` shutdown save (D1). (AC2)
7. Make the persisted settings take effect (assumption A1): `MainWindow` applies the stored theme and gets the `Closing` handler; add `Bootstrap/Steps/SetUILanguageStep.cs`. Confirm risk 3. (AC2)
8. Manual run of the QA checklist in 8.

Frontend tasks: none (no separate frontend stack).

## 8. Test strategy

Use xUnit with hand-rolled fakes (the project's convention: no mocking library), on an STA thread wherever WPF types are created. `ComboBoxItem` needs STA even without a window; reuse the IPX-0003 STA pattern in `tests/.../Views/Windows/StaWindowTestHarness.cs`. New fakes:
- `FakeAppConfigurationManager`: a dictionary-backed `TryGet`/`Set` that records an ordered call log (`Set:<key>`, `Save`) and exposes a configurable `SaveChangesAsync` (completed task by default, or a `TaskCompletionSource` for F1).
- `FakeTranslator`: records `SetCulture` calls.
- `FakeMediator`: records requests.

Reuse `FakeApplicationContext`; move it to a shared `Fakes` location if two namespaces need it.

Test files mirror the source paths: `tests/Nameless.InfoPhoenix.Client.Tests/ViewModels/Pages/`, `.../Features/OpenCurrentLogFile/`, `.../Views/Pages/`.

| AC | Test | Level |
|---|---|---|
| AC1 | **T1:** `new AutoDiscoverableNavigationViewItemProvider([typeof(App).Assembly]).GetItems(Placement.Bottom)` contains exactly one item whose `TargetPageType == typeof(AppConfigurationPage)` (STA). "Independent of bootstrap": the page and VM have no dependency on `IBootstrapper`/splash types, which code review confirms. | Unit |
| AC2 | **T2** (one per setting: Theme, Language, ConfirmBeforeExit): navigate (`OnNavigatedToAsync`), change the property, then **immediately** read back through the manager's extension property (`fake.Theme`, `fake.Language`, `fake.ConfirmBeforeExit`). Assert the new value, and assert the call log ends with `Set:<key>` followed by `Save` (exactly one save). For Language, also assert `FakeTranslator` received `SetCulture("en-GB")`. | Unit (VM) |
| AC2 | **T3:** `OnNavigatedToAsync` alone produces **zero** saves, and so does a second call. **T4:** setting the same value again produces no extra save. **F1:** serialised saves (see 4). | Unit (VM) |
| AC2 | **T5 (integration, optional but recommended):** the real `AppConfigurationManager` over a temp-dir `IFileManager`. Change ConfirmBeforeExit through the VM, await completion by polling `app.config` for up to 5 s, then build a **new** `AppConfigurationManager` on the same folder and assert the value reads back. This proves "persisted before shutdown" end to end. | Integration |
| AC3 | **T6:** with no `logs` directory, the handler publishes a warning snackbar and does not throw. The real `explorer.exe` launch is **manual**: QA opens both locations from an install path that **contains spaces**. | Unit + manual |
| AC4 | VM: `AppVersion == FakeApplicationContext.Version` after navigation. View (optional, STA): construct the page, `FindName("AppVersionTextBlock")` is a `TextBlock` (not a `TextBox`) with that text. If constructing the page in tests is blocked by `{loc:Translator}` without a translator, set `TranslatorExtension.SetTranslator(fakeTranslator)` in that test only, or fall back to the VM assertion plus code review. | Unit / component |

**Risky failure modes for QA to sharpen:**
- Rapid double-toggle of the checkbox, then kill the process (Task Manager). On restart, `app.config` must reflect the final state.
- Switch language while on the page: all page texts change at once, with no `[Key]` placeholders in either language.
- Theme HighContrast on a machine without system high contrast. Theme System, then change the Windows theme (2.6 limitation).
- Open log file when `logs` is empty or missing (warning snackbar), and when started from a shortcut with a different "Start in" (D4).
- Install or data path with spaces and non-ASCII characters (D3).
- Read-only `app.config`: the save fails and is only logged, with no crash. User feedback is deferred to IPX-0006.
- Confirm-before-exit on: closing shows Yes/No/Cancel. No stops asking, and on the next launch it does not ask (persisted through the shutdown save, best-effort).

## 9. Open decisions

None blocking. The ticket owner should confirm one assumption:

- **A1:** the startup restore of theme and language, and the `Closing` handler that honours Confirm-before-exit, are included (task 7) so the saved settings actually take effect. They are direct ports of the sample. If the owner prefers a separate ticket, drop task 7. The page and AC1–AC4 tests do not depend on it, but the settings would then have no effect after a restart.
- Upstream (not this repo): `ProcessHelper` should quote its argument (D3), and the sample's log-file handler opens the oldest file (D5). Both are worth a `Nameless` issue.

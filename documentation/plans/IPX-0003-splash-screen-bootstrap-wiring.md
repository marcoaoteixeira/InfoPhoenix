# IPX-0003 — Splash screen with live bootstrap progress

- Ticket: [IPX-0003-SplashScreenBootstrapWiring (#12)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/12)
- Epic: [IPX-0002-SetupAndInitialization (#11)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11)
- Stack: .NET 10 / WPF (`dotnet`), single project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY** (no separate frontend; the WPF UI lives in the same .NET project).
- UX spec: **not needed** (port of an existing screen; the only visual choices are listed under "Deviations" below).
- ADR: **none** (no framework change, no external integration, no data-model change).

## 1. Context

At the moment `App.OnCurrentHostStart` resolves `INavigationWindow` and shows it directly. No bootstrap run is visible to the user. This ticket adds a modal splash window. It runs `IBootstrapper.RunAsync` while showing live progress, and closes when the run finishes. After that `MainWindow` is shown. The window is a port of
`samples/windows/Nameless.WinApp/Views/Windows/SplashScreenWindow.xaml(.cs)` and the matching lines of the sample's `App.xaml.cs` (read on GitHub `main` on 2026-11-04).

Ground truth checked against the installed packages (`Nameless.Windows` 4.0.0, `Nameless.Common` 4.0.0):

| Type | Where it lives |
|---|---|
| `IBootstrapper.RunAsync(IProgress<StepProgress>, CancellationToken)` | `Nameless.Windows.Bootstrap` |
| `StepProgress` (`StepName`, `Message`, `PercentageComplete`, `Type`, ...) | `Nameless.Windows.Bootstrap.Notification` |
| `BootstrapperOptions.WarmupTimeout` (int, ms) | `Nameless.Windows.Bootstrap` |
| `IWindow` (`Show(WindowStartupLocation)`, `Close()`) | `Nameless.Windows.UI` |
| `IApplicationContext` (`ApplicationName`, `Version`, ...) | `Nameless.Application` (Nameless.Common) |
| `GetSectionFor<T>()` / `GetOrCreate<T>()` | `Nameless.Configuration` (Nameless.Common) |
| **`ISplashScreenWindow`** | **Not in the package.** The sample declares it itself (`public interface ISplashScreenWindow : IWindow;` at the top of `SplashScreenWindow.xaml.cs`). InfoPhoenix must declare it too. |

## 2. Approach

### 2.1 Files

| Path (under `src/Nameless.InfoPhoenix.Client/`) | Change |
|---|---|
| `Views/Windows/SplashScreenWindow.xaml` | New. Port of the sample XAML (see 2.3 for deviations). |
| `Views/Windows/SplashScreenWindow.xaml.cs` | New. Declares `public interface ISplashScreenWindow : IWindow;` and `public partial class SplashScreenWindow : ISplashScreenWindow`, the same as the sample. |
| `Resources/imgs/splash_screen_256x256.png` | New binary. Copy it from `C:\Workspace\InfoPhoenix\resources\imgs\` (see 2.4). |
| `Resources/imgs/application_64x64.png` | New binary, same source. It replaces the sample's `loading_64x64.png` (see 2.3). |
| `Nameless.InfoPhoenix.Client.csproj` | Add `None Remove` + `Resource Include` for both PNGs, following the existing `ValidationResourceDictionary.xaml` pattern. |
| `App.xaml.cs` | In `OnCurrentHostStart`, show the splash between resolving `main` and `main.ShowWindow()`. |

No `ViewModels/Windows/SplashScreenWindowViewModel`: the sample uses code-behind with named elements, and this ticket follows it. The CLAUDE.md "mirror ViewModels under Views" rule only applies when a view model exists. No change to `Directory.Build.props`, `appsettings.json`, `Constants.cs` or `ApplicationHostFeature.cs`.

Placement: `SplashScreenWindow` is a window, so it goes in `Views/Windows/` next to `MainWindow`. The startup wiring stays in `App.xaml.cs`, as in the sample and the current code. It is not a `Features/` slice and needs no `Infrastructure/` host feature.

### 2.2 Behaviour (binding for the developer)

Constructor `SplashScreenWindow(IApplicationContext applicationContext, IBootstrapper bootstrapper, IConfiguration configuration)`:
- Null-guard the arguments as the sample does (`Throws.When.Null`, from `Nameless.Contracts`).
- Create `_progress = new Progress<StepProgress>(StepProgressReporter)`.
- Call `InitializeComponent()`, then `Initialize()`.

`Initialize()`:
- `ApplicationVersionTextBlock.Text = _applicationContext.Version;` (sample)
- `ApplicationNameTextBlock.Text = _applicationContext.ApplicationName;` (**gap fill**: the sample never sets this, so its splash shows the literal placeholder "Application Name". `ApplicationName` comes from `--applicationName=InfoPhoenix` in `BuildHostArgs`.)

`Show(WindowStartupLocation startupLocation)`: set `WindowStartupLocation`, then `ShowDialog()` (sample).

`StepProgressReporter(StepProgress report)`: inside `Dispatcher.Invoke(...)`:
- `StepNameTextBlock.Text = report.StepName;` (**gap fill**: the sample writes `string.Empty`. AC2 requires the step name.)
- `StepMessageTextBlock.Text = report.Message;` (sample)

`SplashScreenReady` (the `ContentRendered` handler, `async void`):
```
var timeout = GetBootstrapTimeout();          // same as sample
using var cts = new CancellationTokenSource(timeout);
await _bootstrapper.RunAsync(_progress, cts.Token);
Close();
```
This differs from the sample on purpose (see 2.3, D1). Use no `ContinueWith`, no `SkipContextSync()`/`ConfigureAwait(false)` and no try/catch. The await resumes on the UI thread, so `Close()` runs there directly, and an exception from `RunAsync` is rethrown on the Dispatcher.

`GetBootstrapTimeout()`: `_configuration.GetSectionFor<BootstrapperOptions>().GetOrCreate<BootstrapperOptions>().WarmupTimeout` (sample).

`App.OnCurrentHostStart`, in this order:
1. `TranslatorExtension.SetTranslator(...)` (unchanged)
2. `var main = provider.GetRequiredService<INavigationWindow>();` (**must stay before the splash**, see 6)
3. `provider.GetRequiredService<ISplashScreenWindow>().Show(WindowStartupLocation.CenterScreen);` (blocks until the splash closes)
4. `main.ShowWindow();`

DI registration: the sample has no explicit registration. `WindowHostFeature` (part of `WinHostFactory.CreateDefault`) is expected to scan `typeof(App).Assembly` for `IWindow` implementations. First check that `GetRequiredService<ISplashScreenWindow>()` resolves at runtime. If it does not, register it as a transient in `ApplicationHostFeature` and report the deviation.

### 2.3 Deviations from the sample (decided here, each a one-line revert)

| # | Sample | InfoPhoenix | Why |
|---|---|---|---|
| D1 | `await RunAsync(...).ContinueWith(_ => Dispatcher.Invoke(Close), cts.Token).SkipContextSync();` | `await RunAsync(...); Close();` | **The sample does not do what AC4 says.** `ContinueWith` without `TaskContinuationOptions` also runs when `RunAsync` faults. It closes the splash, and the awaited task (the continuation) succeeds, so the bootstrap exception is **silently swallowed** and `MainWindow` opens anyway. The ticket text and AC4 require the exception to propagate unhandled, and the plain `await` does exactly that with no try/catch. Side effect: the sample's timeout path also behaves oddly (the continuation is cancelled, so `Close` never runs and a `TaskCanceledException` escapes). With D1 a timeout surfaces as `OperationCanceledException`, the same as any other bootstrap failure. |
| D2 | `StepNameTextBlock.Text = string.Empty;` | `= report.StepName;` | AC2 requires the step name. `StepProgress.StepName` exists in Nameless.Windows 4.0.0, so this fills an oversight and is not a conflict. |
| D3 | `ApplicationNameTextBlock` never set (shows "Application Name") | Set from `IApplicationContext.ApplicationName` | The ticket's Scope lists "application name". |
| D4 | Header icon `pack://.../Resources/imgs/loading_64x64.png` | `pack://application:,,,/Resources/imgs/application_64x64.png` | `loading_64x64.png` does not exist in InfoPhoenix. A missing pack image throws during `InitializeComponent` (XamlParseException). `application_64x64.png` is already in `resources/imgs/`. |
| D5 | `FontFamily="pack://application:,,,/Resources/#Alte Haas Grotesk Bold"` on the name | Remove the `FontFamily` attribute (default font, keep `FontSize`/`FontWeight`) | The font file is not in InfoPhoenix. Vendoring a third-party font is out of scope. A missing font falls back silently, but a dangling pack reference is misleading. |

Everything else in the XAML stays exactly as in the sample: the 640x256 size, `WindowStyle=None`, `ResizeMode=NoResize`, the rounded `Window.Clip`, the `WhiteSmoke` background, the grid layout, the element names, the indeterminate `ForestGreen` progress bar, the `ContentRendered="SplashScreenReady"` handler, and `x:Class="Nameless.InfoPhoenix.Client.Views.Windows.SplashScreenWindow"`.

### 2.4 Image resource placement

- WPF pack URIs `pack://application:,,,/Resources/imgs/<file>` resolve to items with build action `Resource` and the project-relative path `Resources\imgs\<file>`. That means the files must sit inside the project folder, at `src/Nameless.InfoPhoenix.Client/Resources/imgs/`. `Resources/xaml/` already follows this convention.
- **The source images are not in git.** `C:\Workspace\InfoPhoenix\resources\` is untracked in the main checkout and absent from this worktree. Copy the two PNGs from there into `src/Nameless.InfoPhoenix.Client/Resources/imgs/` in the worktree. The copies inside `src/` are the ones that get committed. Leave the repo-root `resources/` folder alone; whether to delete or track it belongs to whoever owns that folder.
- csproj delta (same structure as the existing entries):
  ```xml
  <None Remove="Resources\imgs\application_64x64.png" />
  <None Remove="Resources\imgs\splash_screen_256x256.png" />
  ...
  <Resource Include="Resources\imgs\application_64x64.png" />
  <Resource Include="Resources\imgs\splash_screen_256x256.png" />
  ```
- `Directory.Build.props` needs no change. It sets `UseWPF` for every project, and `Resource` items need nothing else.

## 3. Contract

There is no HTTP API or event. The only contract is in-process:

```csharp
namespace Nameless.InfoPhoenix.Client.Views.Windows;
public interface ISplashScreenWindow : IWindow;   // Nameless.Windows.UI.IWindow
```
- `Show(WindowStartupLocation)`: modal. It returns only after the bootstrap run has completed and the window has closed.
- Named elements: `ApplicationNameTextBlock`, `StepNameTextBlock`, `StepMessageTextBlock`, `ApplicationVersionTextBlock`. Tests look them up with `FindName`, so the names are part of the contract.

The change is additive and not breaking.

## 4. Quality attributes

| Attribute | How the design serves it | Fitness function |
|---|---|---|
| Correctness of startup order (operability) | `main` is resolved before the splash, so WPF makes `MainWindow` the `Application.MainWindow` and closing the splash does not trigger `OnLastWindowClose` shutdown. | Covered by code review plus a manual run. It is not cheap to automate because `App` holds a static host. |
| Fail-loud on bootstrap failure | D1: the plain `await` lets the exception propagate, with no swallowing `ContinueWith`. | Test T4 (below) asserts that the bootstrapper's exception reaches `Dispatcher.UnhandledException`. |
| Responsiveness | The bootstrap runs async after `ContentRendered`, and progress is marshalled through `Dispatcher.Invoke`. The UI thread is never blocked by step work. | Test T2 asserts that text changes *while* `RunAsync` is still in flight. |
| Packaging integrity | The images are `Resource` items, and a missing one fails `InitializeComponent` in tests. | Every window test constructs the real XAML, so a missing or misnamed resource fails the test suite. |

## 5. Security notes

There is no new entry point, no network access and no persisted data. `StepProgress.Message` is shown as plain `TextBlock.Text`, so there is no markup injection. Step messages should never carry secrets such as connection strings. That matters for whoever writes steps later, but this ticket defines none. There is nothing else to threat-model.

## 6. Risks

1. **No bootstrap steps exist yet.** InfoPhoenix has no `IStep` implementations, so in the real app `RunAsync` finishes almost at once. The splash flashes for a frame and the step and message texts keep their XAML placeholders ("Step", "Step progress..."). That is correct per the ticket (no minimum duration, steps are out of scope). QA must verify AC2 with the test, not by eye. Also check on the first manual run that `Bootstrapper.RunAsync` with zero steps does not throw. If it does, that is a package issue: report it, do not work around it here.
2. **`WarmupTimeout` default.** `appsettings.json` has no bootstrapper section (the sample has none either), so `GetOrCreate` returns the default. If the default were `0`, the token would be cancelled immediately. Check the package default when wiring up. If it is unusable, report it before adding config.
3. **Ordering in `OnCurrentHostStart` is load-bearing.** If the splash were resolved or shown before `INavigationWindow`, it would become `Application.MainWindow`, and closing it would shut the app down (default `ShutdownMode=OnLastWindowClose`).
4. **Progress after close.** `Progress<T>` posts asynchronously, so a final report can land after `Close()`. Setting `Text` on a closed window is harmless. Tests must not assume the *last* report is visible after `ShowDialog` returns (see T2).
5. **WPF in xUnit.** Window tests need an STA thread with its own Dispatcher. Pack URIs resolve against `Application.ResourceAssembly`. Without an `Application` that is the test host's entry assembly, so tests must set `Application.ResourceAssembly = typeof(App).Assembly` once per process (the setter allows a single assignment, so guard it in a shared static fixture). Do **not** instantiate `App`, because its static `CurrentHost` builds the whole host.
6. **AC4 user impact.** A failing step crashes the app before `MainWindow` with the default WPF unhandled-exception behaviour. That is what the ticket asks for, and error UI is out of scope.

## 7. Work split and tasks (backend-developer-dotnet)

Build order:

1. Copy the two PNGs into `src/Nameless.InfoPhoenix.Client/Resources/imgs/` and add the csproj `None Remove`/`Resource` entries. (AC1)
2. Add `Views/Windows/SplashScreenWindow.xaml` as a port of the sample with D4 and D5 applied. (AC1, AC2)
3. Add `Views/Windows/SplashScreenWindow.xaml.cs` with `ISplashScreenWindow`, the constructor, `Initialize` (D3), `Show`, `StepProgressReporter` (D2), `SplashScreenReady` (D1) and `GetBootstrapTimeout`. (AC1–AC4)
4. Wire it into `App.OnCurrentHostStart`: resolve `main`, then show the splash centred, then `main.ShowWindow()`. Confirm DI resolution, and check risks 1 and 2 with one manual run. (AC1, AC3)
5. Write tests T1–T4 in `tests/Nameless.InfoPhoenix.Client.Tests/Views/Windows/SplashScreenWindowTests.cs`, mirroring the source path. Use hand-rolled fakes for `IApplicationContext` and `IBootstrapper`, and an in-memory `ConfigurationBuilder` for `IConfiguration`. Add no new mocking package. If the test project cannot reach `Microsoft.Extensions.Configuration` (in-memory) transitively, add only that package through `Directory.Packages.props`. (AC2–AC4)

Frontend tasks: none.

## 8. Test strategy

All of these are component tests that drive the real `SplashScreenWindow` on a dedicated STA thread (start a `Thread`, call `SetApartmentState(STA)`, run the body, call `Dispatcher.InvokeShutdown()`, and rethrow any captured exception on the test thread).

| AC | Test | Notes |
|---|---|---|
| AC1 | T1: `Show(CenterScreen)` sets `WindowStartupLocation == CenterScreen` and is modal (it returns only after the fake bootstrapper completes). The order splash → `main.ShowWindow()` is checked by code review and a manual run. | `App` is static and not unit-testable without invasive refactoring, so none is planned. |
| AC2 | T2: fake `IBootstrapper.RunAsync` reports `new StepProgress { StepName = "S1", Message = "M1" }`. It then **waits**, polling `window.Dispatcher.Invoke(() => ((TextBlock)window.FindName("StepNameTextBlock")).Text)` until it equals `"S1"` and the message equals `"M1"` (bounded, e.g. 5 s, fail on timeout), then completes. It also asserts `ApplicationVersionTextBlock`/`ApplicationNameTextBlock` equal the fake context's values. | Waiting inside the fake proves the update happens *during* the run, not afterwards (risk 4). A second report "S2"/"M2" proves the texts keep updating. |
| AC3 | T3: the fake completes at once. `ShowDialog` returns (that is the assertion), and the window is closed (`IsLoaded == false` or the `Closed` event fired). | "Immediately" means no minimum duration: no delay exists in the code path. |
| AC4 | T4: the fake throws `InvalidOperationException("boom")`. The test subscribes to `window.Dispatcher.UnhandledException`, records `e.Exception`, sets `Handled = true` and closes the window so that `ShowDialog` returns. It asserts the recorded exception is that same instance, which proves there is no swallowing `ContinueWith` and no try/catch. | Under the sample's code this test would fail, which is exactly what D1 fixes. |

For QA to sharpen: the timeout path (the fake honours the token, with the configured `WarmupTimeout` small, and the `OperationCanceledException` propagates), multiple rapid reports, a report with a null `Message`, and a manual check that the PNGs render in the published `win-x64` build.

## 9. Open decisions

None blocking. D1–D5 above are decisions taken within the ticket's scope, and each is a one-line revert. D1 is the one worth a second look by the ticket owner: the ticket justifies AC4 as "matches the reference sample", but the sample actually swallows the exception. This plan follows the AC, not the sample. The sample itself (`Nameless` repo) probably has the same bug. Fixing it there is a separate change in another repo.

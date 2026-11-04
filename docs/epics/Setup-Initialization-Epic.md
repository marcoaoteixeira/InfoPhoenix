# Epic (DRAFT): Setup & Initialization

| Field   | Value |
|---------|-------|
| Tracker | [#11](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11) |
| Team    | Nameless |
| Project/Repo | GitHub `marcoaoteixeira/InfoPhoenix` (Project board "InfoPhoenix", #2) |
| Owner   | Marco Teixeira |
| Status  | Brainstorming — both parts closed, ready for tracker-ready draft |
| Updated | 2026-11-04 |

> This draft contains only what is confirmed by the product owner, sources, or existing tickets.
> Anything not confirmed is listed under **Open Questions** rather than assumed.
> Work proceeds part by part.

---

## 1. Summary

InfoPhoenix (WPF rewrite of legacy InfoFenix) needs its startup/setup experience built: a splash screen shown during app bootstrap, and an application configuration page modeled on the `AppConfigurationPage` in the `Nameless.WinApp` reference sample. Two parts:
1. Splash Screen
2. Application Configuration Page

## 2. Decisions (confirmed by product owner)

| # | Decision |
|---|----------|
| *(none yet)* | |

## 3. Background (what exists today)

**InfoPhoenix repo** — current scaffold only:
- `App.xaml(.cs)`, `appsettings.json`, `Constants.cs`
- `Infrastructure/ApplicationHostFeature.cs` (one host feature so far)
- `Data/AppDbContext.cs`
- `ViewModels/Windows/MainWindowViewModel.cs` + matching `Views/Windows/MainWindow.xaml`
- No `Features/` folder yet. No splash screen. No config page.

**GitHub tracker state:**
- Repo has one issue total: `#9 IPX-0001-Initialize` (closed) — initial scaffold commit, unrelated to this epic.
- Project board "InfoPhoenix" (#2) has zero items.
- No existing epic/issue named "Setup & Initialization" — this is a fresh epic.

**Nameless.WinApp reference sample** — [github.com/marcoaoteixeira/Nameless/tree/main/samples/windows/Nameless.WinApp](https://github.com/marcoaoteixeira/Nameless/tree/main/samples/windows/Nameless.WinApp):
- `Views/Windows/SplashScreenWindow.xaml(.cs)` exists — a custom Window (not WPF's built-in `SplashScreen` image), sits beside `MainWindow.xaml`. Exact wiring to host startup **not yet confirmed** (file contents not read).
- `Views/Pages/AppConfigurationPage.xaml(.cs)` exists as a Page. No matching ViewModel file was found at a glance. Bound settings/persistence **not yet confirmed**.
- `Bootstrap/Steps/` has `CleanupPreviousSystemUpdateStep`, `EnsureApplicationDirectoriesExistenceStep`, `InitializeLuceneIndexStep`, `SetUILanguageStep` — hints at bootstrap sequence and a UI-language setting, not confirmed as splash-screen-driving.
- `Infrastructure/` has `ApplicationHostFeature`, `SystemUpdateHostFeature`, `CustomFileManagerHostFeature` — hints at update-related config; InfoPhoenix has no update feature yet, so importing this 1:1 is not assumed.
- `Features/` in the sample: `CheckSystemUpdate`, `OpenCurrentLogFile`, `PerformApplicationBackup`, `PerformApplicationRestore`, `PerformSystemUpdate` — none of these are configuration-page features; no direct precedent found there.

**Related epics/tickets (context only):** none.

**PO review (checkpoint 1) — risks flagged:**
- Epic goal not stated by user; inferred as "give InfoPhoenix a first-run/startup experience — visible bootstrap feedback + a config page" — **open question**, could be read narrower (just wiring two screens) or broader (onboarding/first-run wizard).
- Both parts are "modeled on" a sample whose exact wiring/settings are unconfirmed — biggest risk to scope/AC until read.
- The two parts look independent (no shared state) — worth confirming they're two separate tickets, not one combined "first-run experience" story.

## 4. Part 1 — Splash Screen

**Goal:** Give visible branding feedback while the app bootstraps, reusing the `Nameless.WinApp` sample's splash mechanism.

**Confirmed:** D1–D5 (D1–D3, D5 revised this round — see below).

### Ground truth read from the sample (`Views/Windows/SplashScreenWindow.xaml(.cs)`, `App.xaml.cs`)

- It's a real `Window` (not WPF's native `SplashScreen` feature): 640×256, borderless, shown via `ISplashScreenWindow.Show(CenterScreen)` → `ShowDialog()` (modal) from `App.xaml.cs`'s `OnCurrentHostStart`, **before** `MainWindow` is shown.
- Content is **dynamic**, not static: `ApplicationNameTextBlock`, `StepNameTextBlock`, `StepMessageTextBlock` (bound to `IBootstrapper.RunAsync`'s progress reporter — writes each step's `Message`), an indeterminate `ProgressBar`, `ApplicationVersionTextBlock` (from `IApplicationContext.Version`). Background image is `splash_screen_256x256.png`.
- `SplashScreenReady` (on `ContentRendered`) builds a `CancellationTokenSource` from `BootstrapperOptions.WarmupTimeout`, awaits `_bootstrapper.RunAsync(progress, ct)`, then `Dispatcher.Invoke(Close)` — i.e. it **is** the thing that drives the bootstrap steps, not a passive splash shown alongside them.
- **No try/catch anywhere in this file.** An exception from `RunAsync` is not handled by the splash screen itself in the sample — it propagates up (behavior higher up `App.xaml.cs` not confirmed).

### Answered — Part 1 (resolved conflicts)

| # | Question | Answer |
|---|----------|--------|
| ~~Q1.6~~ | Full sample behavior (modal Window, dynamic step/progress/version text, drives `IBootstrapper.RunAsync`) with image swapped, or chrome-only + static branding? | Full sample behavior, image swapped → supersedes D1/D2 |
| ~~Q1.7~~ | `MessageBox`-then-stop added on top, or mirror the sample exactly (no handling in this file)? | Mirror exactly — no try/catch in the splash itself → supersedes D3 |

### Decisions

| # | Decision |
|---|----------|
| ~~D1~~ | ~~Use WPF's built-in `SplashScreen` feature, not a custom Window.~~ **Superseded by D1′.** |
| D1′ | **Reuse the sample's `SplashScreenWindow` mechanism exactly:** modal custom `Window`, shown via `Show(CenterScreen)` → `ShowDialog()` from host-start (before `MainWindow`), `ContentRendered` kicks off `_bootstrapper.RunAsync(progress, ct)` with a `BootstrapperOptions.WarmupTimeout`-based cancellation token, closes via `Dispatcher.Invoke(Close)` when done. (Q1.1, Q1.6) |
| ~~D2~~ | ~~Static branding only, no dynamic/per-step text.~~ **Superseded by D2′.** |
| D2′ | **Dynamic content, same as the sample:** `ApplicationNameTextBlock`, `StepNameTextBlock`, `StepMessageTextBlock` (bound to bootstrap progress), indeterminate `ProgressBar`, `ApplicationVersionTextBlock`. Only the background image changes, to `resources/imgs/splash_screen_256x256.png`. (Q1.2, Q1.6) |
| ~~D3~~ | ~~Bootstrap failure shows a `MessageBox`, then stops the app, handled in the splash.~~ **Superseded by D3′.** |
| D3′ | **Mirror the sample exactly: no explicit error handling in the splash/bootstrap-driving code.** Any exception from `_bootstrapper.RunAsync` propagates up unhandled, same as the sample. (Q1.3, Q1.7) |
| D4 | **No minimum display duration** — closes as soon as bootstrap completes (the sample does this too via `Dispatcher.Invoke(Close)`). (Q1.4) |
| ~~D5~~ | ~~Scope: image + wiring only, bootstrap steps out of scope.~~ **Superseded by D5′.** |
| D5′ | **Scope now includes the bootstrapper hookup itself** (`ISplashScreenWindow`, `IBootstrapper.RunAsync` wiring, `BootstrapperOptions`), since that's inseparable from reusing the sample's mechanism exactly. Defining the *individual bootstrap steps' content* (DB init, Lucene index init, etc.) remains out of scope — only the driving mechanism is in. (Q1.5, Q1.6) |

**Part 1: closed.**

## 5. Part 2 — Application Configuration Page

**Goal:** Give the user a settings page reached via navigation, matching the sample's `AppConfigurationPage` pattern (confirmed scope below), with InfoPhoenix-specific invalid-state handling.

**Confirmed:** D6–D11.

### Ground truth read from the sample (`Views/Pages/AppConfigurationPage.xaml(.cs)`, `AppConfigurationPageViewModel.cs`, `App.xaml.cs`)

- Reached via `[NavigationViewItem(Icon = Settings48, Title = ..., Placement = Placement.Bottom)]` on the page — menu item in nav, no forced first-run. No Save button; each control's `OnChanged` partial applies and saves immediately (no batch/confirm step).
- Settings exposed, one `ui:CardControl` each: **Version** (read-only) + Check-for-update button · **Language** (ComboBox, hard-coded `pt-BR`/`en-GB` in the sample) · **Backup** button · **Restore** button · **Theme** (ComboBox: System/Light/Dark/HighContrast) · **Confirm before exit** (checkbox) · **Open app data directory** button · **Open log file** button.
- Persistence: **not** a literal `app.config`/`System.Configuration` file. It's `IAppConfigurationManager` (from the `Nameless.Windows` library — Theme/Language/ConfirmBeforeExit properties), saved via `SaveChangesAsync` — the sample calls this only once, in `App.xaml.cs`'s `OnCurrentHostStop` (i.e. on app shutdown, not per-change, despite the ViewModel applying changes live in-memory).
- No validation/invalid-state handling exists in the sample at all — the only fallback found is language resolving to `CurrentUICulture` if the stored code doesn't match a known culture. No snackbar, no defaults-rebuild behavior.
- `appsettings.json` in the sample has **no** app/theme/language/splash settings — those all live in `IAppConfigurationManager`'s own store (implementation not in this sample, lives in `Nameless.Windows`).

### Answered — Part 2 (resolved conflicts)

| # | Question | Answer |
|---|----------|--------|
| ~~Q2.7~~ | Literal `app.config`, or `IAppConfigurationManager` same as the sample? | `IAppConfigurationManager`, same as the sample → D10 |
| ~~Q2.8~~ | Backup/Restore/Check-for-update cards — include, defer, or drop? | Include, stubbed → D11 |

### Decisions

| # | Decision |
|---|----------|
| D6 | **Settings exposed:** Version (read-only), Language, Theme, Confirm-before-exit — same set/behavior as the sample's cards. Open-app-data-directory and open-log-file buttons also included. (Q2.1, Q2.3) |
| D7 | **Entry point:** reachable via navigation menu item only (same as the sample's `NavigationViewItem` attribute) — no forced first-run, no separate settings icon elsewhere. (Q2.4, Q2.6) |
| D8 | **Reachability:** available anytime post-launch, independent of the splash/bootstrap sequence — Part 1 and Part 2 are independent tickets. (Q2.6) |
| D9 | **Invalid state (InfoPhoenix-specific, beyond the sample):** on an invalid/unreadable setting, show a snackbar error notification and rebuild settings with defaults. This is new behavior — the sample has no such handling. (Q2.5) |
| D10 | **Persistence:** `IAppConfigurationManager` (from `Nameless.Windows`), same mechanism as the sample — not a literal `app.config`/`System.Configuration` file. Changes apply live in-memory per control; save point (e.g. on `OnStop`, same as the sample, vs. per-change) to settle at refinement. (Q2.2, Q2.7) |
| D11 | **Backup / Restore / Check-for-update cards:** included on the page now, **stubbed** (UI present, wired to commands that are no-ops or clearly marked not-yet-implemented) since the underlying features don't exist in InfoPhoenix yet. Real implementation of those features is a separate, later epic. (Q2.8) |

**Part 2: closed.**

## 6. Dependencies

| Dependency | Owner | Source |
|------------|-------|--------|
| Epic goal confirmation (first-run/startup experience scope) | User | PO checkpoint 1 |

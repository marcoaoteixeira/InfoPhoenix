# Setup & Initialization

**Type:** Epic · **Key:** [#11](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11) · **Project/Repo:** GitHub `marcoaoteixeira/InfoPhoenix` (Project board "InfoPhoenix", #2) · **Team:** Nameless
**Source:** brainstorming doc `Setup-Initialization-Epic.md` (decision refs D1′–D11)

---

## Summary

InfoPhoenix needs its startup/setup experience built: a splash screen shown while the app bootstraps, and an application configuration page the user can navigate to at any time. Both reuse the `Nameless.WinApp` sample's mechanisms directly, with only InfoPhoenix-specific changes (the splash image, and snackbar/defaults-rebuild handling on invalid settings). **Confirmed goal (narrow, not a first-run/onboarding wizard):** wire these two screens as independent, always-reachable features — the user sees the app is starting instead of a frozen window, and gets one place to control language/theme/exit-confirmation without being forced through any setup flow.

This epic:
1. **Splash Screen** — reuse the sample's modal `SplashScreenWindow` mechanism exactly (dynamic step/progress/version text, drives the bootstrapper), swapping in InfoPhoenix's own splash image. User-facing point: visible proof the app is starting, not a frozen window.
2. **Application Configuration Page** — reuse the sample's `AppConfigurationPage` (Version, Language, Theme, Confirm-before-exit, open-directory/log-file, plus stubbed Backup/Restore/Check-for-update cards), reached via nav menu, persisted through `IAppConfigurationManager`, with added invalid-state recovery. User-facing point: one discoverable place to control these settings, anytime, with no forced wizard.

## Background

- **Current state:** InfoPhoenix scaffold has `App.xaml(.cs)`, one `Infrastructure/ApplicationHostFeature.cs`, `Data/AppDbContext.cs`, and `MainWindow` only. No splash screen, no config page, no `Features/` folder yet. No existing tracker epic or board items for this work (only unrelated closed issue `#9 IPX-0001-Initialize`, the scaffold commit).
- **Reference implementation** — [Nameless.WinApp sample](https://github.com/marcoaoteixeira/Nameless/tree/main/samples/windows/Nameless.WinApp): both features exist there as the pattern to copy — `Views/Windows/SplashScreenWindow.xaml(.cs)` and `Views/Pages/AppConfigurationPage.xaml(.cs)` + `ViewModels/Pages/AppConfigurationPageViewModel.cs`.
- **Existing building blocks reused:** `Nameless.Windows`'s `ISplashScreenWindow`, `IBootstrapper`/`BootstrapperOptions`, `IApplicationContext`, `IAppConfigurationManager`, `WinHostFactory`/`OnStart` hook in `App.xaml.cs`.

## Flow

```
[App start] -> SplashScreenWindow.Show(CenterScreen) -> ShowDialog() [modal]
                        |
             ContentRendered -> IBootstrapper.RunAsync(progress, ct)
                        |         (ct from BootstrapperOptions.WarmupTimeout)
             step progress -> StepName/StepMessage/ProgressBar/Version text update
                        |
             RunAsync finishes -> Dispatcher.Invoke(Close)
                        |
                  MainWindow shown
                        |
         (any time after) -> user navigates to AppConfigurationPage via nav menu
                        |
   Version | Language | Theme | ConfirmBeforeExit | OpenDataDir | OpenLogFile
   Backup* | Restore* | CheckForUpdate*   (*stubbed — no-op, not yet implemented)
                        |
   change applied live in-memory -> IAppConfigurationManager -> SaveChangesAsync
                        |
   invalid/unreadable setting -> snackbar error notification -> rebuild defaults
```

## Scope

### 1. Splash Screen
- **Mechanism:** modal custom `Window` (`ISplashScreenWindow`), shown via `Show(CenterScreen)` → `ShowDialog()` from host-start, before `MainWindow` is shown.
- **Content:** dynamic — application name, current step name, step message, indeterminate progress bar, application version. Background image: `resources/imgs/splash_screen_256x256.png` (InfoPhoenix's own asset; this is the only visual difference from the sample).
- **Bootstrap wiring:** `ContentRendered` triggers `IBootstrapper.RunAsync(progress, ct)`, where `ct` is built from `BootstrapperOptions.WarmupTimeout`. Closes via `Dispatcher.Invoke(Close)` once `RunAsync` completes — no minimum display duration.
- **Error handling:** none in the splash/bootstrap-driving code — mirrors the sample exactly. Any exception from `RunAsync` propagates up unhandled.
- **Explicitly out of scope:** defining or changing the content of individual bootstrap steps (DB init, Lucene index init, etc.) — only the driving mechanism (`ISplashScreenWindow` + `IBootstrapper` wiring) is in scope here.

### 2. Application Configuration Page
- **Settings exposed:** Version (read-only), Language (combo), Theme (combo: System/Light/Dark/HighContrast), Confirm-before-exit (checkbox), Open-app-data-directory button, Open-log-file button.
- **Stubbed cards:** Backup, Restore, Check-for-update — UI present and wired to commands, but no-ops / clearly marked not-yet-implemented, since the underlying features don't exist in InfoPhoenix yet.
- **Entry point:** navigation menu item only (same `NavigationViewItem` attribute pattern as the sample) — no forced first-run, no other entry point. Reachable at any time post-launch, independent of the splash/bootstrap sequence.
- **Persistence:** `IAppConfigurationManager` (`Nameless.Windows`) — not a literal `app.config`/`System.Configuration` file. Each setting applies and saves (`SaveChangesAsync`) immediately per control change — not batched to app shutdown.
- **Language values:** `pt-BR` and `en-GB`, same as the sample.
- **Invalid state (new vs. the sample):** on an invalid/unreadable setting, show a snackbar error notification and rebuild settings with defaults.

## Out of scope
- Implementing the actual Backup, Restore, and Check-for-update features (separate, later epic) — this epic only stubs their UI entry points.
- Defining/changing individual bootstrap step content (DB init, Lucene index init, etc.).
- Any first-run/onboarding wizard — neither part forces navigation; both are independent, reachable features.

## Dependencies
| Dependency | Owner |
|------------|-------|
| `Nameless.Windows` package providing `ISplashScreenWindow`, `IBootstrapper`, `IAppConfigurationManager` | Nameless (private package) |
| `resources/imgs/splash_screen_256x256.png` asset | already present in repo |

## To settle at refinement
- Exact wording/placement for the "stubbed" state of Backup/Restore/Check-for-update (disabled control vs. clickable no-op vs. "coming soon" label) — split into its own candidate ticket below so it doesn't block the main settings ticket.
- Fixture/method QA will use to trigger "invalid/unreadable setting" for the invalid-state ticket (corrupt file, wrong type, missing key) — not blocking, just needs agreement before that ticket's tests are written.

## Candidate tickets

| Area | Ticket | Acceptance criteria |
|------|--------|---------------------|
| Splash Screen | FE: Implement `SplashScreenWindow` + host-start wiring | Modal window shows on app start before `MainWindow`; displays app name, live step name/message during bootstrap (bootstrap progress reporting verifiably reaches the splash UI), indeterminate progress, app version; background image is `splash_screen_256x256.png`; closes immediately when bootstrap completes; no error handling added (unhandled exceptions propagate, matching the sample). Covered by tests for progress-reaches-UI and close-on-completion. |
| Application Configuration Page | FE: Implement `AppConfigurationPage` + ViewModel (live settings) | Page reachable only via nav menu item, at any time post-launch; exposes Version (read-only), Language (`pt-BR`/`en-GB`), Theme, Confirm-before-exit, Open-data-directory, Open-log-file; each setting change calls `SaveChangesAsync` immediately (not batched). Covered by tests verifying each setting round-trips through `IAppConfigurationManager` right after the change, before app shutdown. |
| Application Configuration Page | FE: Stub Backup/Restore/Check-for-update cards | See "to settle at refinement" — exact stub UX treatment pending; split out so it doesn't block the live-settings ticket above. |
| Application Configuration Page | FE: Invalid-state recovery | When a stored setting is invalid/unreadable, a snackbar error notification appears and settings are rebuilt with defaults, without crashing the page. Covered by a test using an agreed-upon invalid-fixture (corrupt/missing value — to be agreed with QA, not blocking). |

## References
- Linked docs: none
- Tracker: [#11](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11)

# Setup & Initialization — Tickets (epic #11)

Q1 (stub UX) → **disabled control + "coming soon" tooltip/badge**.
Q2 (invalid-setting test fixture) → **mocked/faked `IAppConfigurationManager`** that throws or returns invalid data on load.

| # | Key | Status |
|---|-----|--------|
| 1 | [#12](https://github.com/marcoaoteixeira/InfoPhoenix/issues/12) IPX-0003-SplashScreenBootstrapWiring | created, linked to epic #11, on board |
| 2 | [#13](https://github.com/marcoaoteixeira/InfoPhoenix/issues/13) IPX-0004-AppConfigurationPageLiveSettings | created, linked to epic #11, on board |
| 3 | [#14](https://github.com/marcoaoteixeira/InfoPhoenix/issues/14) IPX-0005-StubBackupRestoreCheckForUpdateCards | created, linked to epic #11, on board |
| 4 | [#15](https://github.com/marcoaoteixeira/InfoPhoenix/issues/15) IPX-0006-AppConfigurationInvalidStateRecovery | created, linked to epic #11, on board |

---

## 1. [Story] IPX-0003-SplashScreenBootstrapWiring

**Story:** As an InfoPhoenix user, I want to see a splash screen with live startup progress instead of a frozen window, so I know the app is actually starting and roughly what it's doing.

**Value:** Removes the "is it frozen?" moment at launch.

**Context:** No splash screen exists today; `App.xaml.cs` shows `MainWindow` directly. Mirrors the `Nameless.WinApp` sample's `SplashScreenWindow` exactly (modal custom `Window`, not WPF's built-in `SplashScreen`), only the background image is InfoPhoenix-specific (`resources/imgs/splash_screen_256x256.png`, already in the repo).

**Scope:**
- Modal splash window shown via `Show(CenterScreen)` → `ShowDialog()` from host-start, before `MainWindow` appears.
- Displays: application name, current bootstrap step name, step message, indeterminate progress bar, application version — all updating live as bootstrap runs.
- `ContentRendered` triggers the bootstrap run with a timeout-bound cancellation token; window closes as soon as bootstrap completes. No minimum display duration.
- No error handling added — an exception from the bootstrap run propagates unhandled, matching the sample.

**Out of scope:** Defining/changing what any individual bootstrap step does. Any try/catch or error UI for bootstrap failures.

**Acceptance criteria:**
- [ ] Splash window is shown, centered, before `MainWindow` becomes visible.
- [ ] As bootstrap runs, step name, step message and application version shown on the splash update live (progress genuinely reaches the UI — verifiable in a test, not hardcoded text).
- [ ] When bootstrap completes, the splash closes immediately and `MainWindow` is shown.
- [ ] If bootstrap throws, the exception propagates unhandled (no try/catch here) — matches the reference sample.

**Assumptions:** `Nameless.Windows` already exposes `ISplashScreenWindow`, `IBootstrapper`, `IApplicationContext`, `BootstrapperOptions` as used by the sample; no changes needed to those contracts.

**Dependencies:** None.

**Notes for team:** Copy the sample's `SplashScreenWindow.xaml(.cs)` and host-start wiring in `App.xaml.cs` closely; only intentional diff is the background image. QA/architect: the "progress reaches the UI" AC needs a test seam to drive/observe progress without a real multi-second bootstrap.

---

## 2. [Story] IPX-0004-AppConfigurationPageLiveSettings

**Story:** As an InfoPhoenix user, I want a settings page I can reach any time to control language, theme, exit-confirmation, and open my data/log locations, so I don't need a forced setup wizard to change these.

**Value:** Delivers the core, always-reachable settings experience.

**Context:** No config page exists today, no `Features/` folder yet. Modeled on the sample's `AppConfigurationPage`/`AppConfigurationPageViewModel`, persisted through `IAppConfigurationManager` (not a literal `app.config`) — diverging from the sample on save timing: InfoPhoenix saves each change immediately, not batched to shutdown.

**Scope:**
- Page reachable only via a navigation menu item, at any time after launch, independent of the splash/bootstrap sequence.
- Exposes: Version (read-only), Language (`pt-BR`/`en-GB`), Theme (System/Light/Dark/HighContrast), Confirm-before-exit (checkbox), Open-app-data-directory (button), Open-log-file (button).
- Each setting change applies live in-memory and calls `SaveChangesAsync` immediately on that same change — not batched.

**Out of scope:** Backup/Restore/Check-for-update cards (IPX-0005). Invalid/unreadable-setting recovery (IPX-0006). Any first-run/forced-navigation entry point.

**Acceptance criteria:**
- [ ] Nav menu has a settings entry that opens the configuration page, independent of bootstrap state.
- [ ] Changing Language, Theme, or Confirm-before-exit applies the value live and persists via `IAppConfigurationManager.SaveChangesAsync` immediately — verifiable by reading the setting back right after the change, before any app shutdown.
- [ ] Open-app-data-directory / Open-log-file buttons open the corresponding location in the OS file explorer / default viewer.
- [ ] Version field shows the current application version and is not editable.

**Assumptions:** `IAppConfigurationManager` already supports per-change `SaveChangesAsync` without requiring a batch/commit step.

**Dependencies:** None required from IPX-0003, though both land on the nav shell — coordinate if the nav menu doesn't exist yet.

**Notes for team:** Architect: confirm whether applying+saving on every control change (vs. the sample's save-on-shutdown) has any race/perf implication worth a note. Mirror `ViewModels/Pages/AppConfigurationPageViewModel.cs` under `Views/Pages/AppConfigurationPage.xaml(.cs)` per the project's folder convention.

---

## 3. [Story] IPX-0005-StubBackupRestoreCheckForUpdateCards

**Story:** As an InfoPhoenix user, I want to see that Backup, Restore, and Check-for-update exist but aren't available yet, so I'm not confused about missing functionality or left clicking something that silently does nothing.

**Value:** Sets correct expectations for features that don't exist yet, without implying they're broken.

**Context:** The sample has working Backup/Restore/Check-for-update cards; InfoPhoenix has none of those underlying features yet (separate, later epic). This ticket only adds UI entry points in a clearly-not-yet-implemented state.

**Scope:** Add the three cards to the configuration page (same page as IPX-0004) **disabled**, with a **"coming soon" tooltip/badge**.

**Out of scope:** Any real Backup/Restore/Check-for-update logic — separate, later epic.

**Acceptance criteria:**
- [ ] Backup, Restore, and Check-for-update cards are visibly present but disabled, with a "coming soon" indicator.
- [ ] Attempting to interact with a disabled card executes no command (no silent no-op action, no exception).

**Dependencies:** Builds on the page created in IPX-0004 — develop after or alongside it, not before.

**Notes for team:** UX specialist: a one-line review of the "coming soon" treatment (placement, wording, badge vs. tooltip) would remove ambiguity — lightweight, no full flow spec needed.

---

## 4. [Story] IPX-0006-AppConfigurationInvalidStateRecovery

**Story:** As an InfoPhoenix user, I want the app to recover gracefully if my saved settings are invalid or unreadable, so a corrupted settings file doesn't crash the app or leave it unusable.

**Value:** Protects IPX-0004's core promise from being undermined by bad stored data.

**Context:** The reference sample has no such handling at all (only a language-resolution fallback to `CurrentUICulture`). This is new, InfoPhoenix-specific behavior.

**Scope:** When `IAppConfigurationManager` reports a setting as invalid or unreadable on load, show a snackbar error notification and rebuild the settings with default values, without crashing the configuration page or the app.

**Out of scope:** On-disk format of the settings store, or any migration/versioning of settings — belongs to `Nameless.Windows`.

**Acceptance criteria:**
- [ ] When a stored setting is invalid or unreadable, loading settings shows a snackbar error notification.
- [ ] After recovery, all settings reset to defaults and the configuration page remains fully usable (no crash, no stuck/blank state).
- [ ] When settings are valid, no snackbar appears and no reset happens (recovery path doesn't fire on the happy path).
- [ ] Test uses a mocked/faked `IAppConfigurationManager` that throws or returns invalid data on load (not a real corrupt on-disk file).

**Dependencies:** IPX-0004 (the live-settings page/ViewModel this recovery wraps).

**Notes for team:** Architect: confirm where "invalid/unreadable" is actually detected (inside `IAppConfigurationManager` vs. the ViewModel catching a thrown exception) — decides where this logic lives and the test seam.

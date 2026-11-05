# Epic (DRAFT): Backup & Restore

| Field   | Value |
|---------|-------|
| Tracker | [IPX-0007](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23) |
| Team    | Solo (marcoaoteixeira) |
| Project/Repo | GitHub `marcoaoteixeira/InfoPhoenix` |
| Owner   | marcoaoteixeira |
| Status  | Written to tracker — ready for ticket breakdown |
| Updated | 2026-11-05 |

> This draft contains only what is confirmed by the product owner, sources, or existing tickets.
> Anything not confirmed is listed under **Open Questions** rather than assumed.
> Work proceeds part by part.

---

## 1. Summary

Build real Backup and Restore features for InfoPhoenix (desktop, solo-dev, no server/cloud).
Covers: creating a backup and restoring from one. Backed-up data: SQLite DB, Lucene index,
app configuration. Parts: **Backup creation**, **Restore flow**.

## 2. Decisions (confirmed by product owner)

| # | Decision |
|---|----------|
| D1 | **Epic covers Backup creation and Restore flow only.** "Backup management" (browse/list/delete past backups) and "schedule/settings config" were not selected as parts. |
| D2 | **Backed-up/restored data = SQLite DB file + Lucene index directory + app configuration.** All three, nothing else. |
| D3 | **Solo team, desktop/local app.** No server, no account, no cloud sync in scope. |
| D4 | **Backup trigger is manual only.** A "Back up now" button (the existing stub card) — no scheduling in this epic. (Q1.1) |
| D5 | **Backup destination is a fixed app-managed folder.** No save dialog / user-chosen path. (Q1.2) |
| D6 | **Backup archive is one bundled file (zip)** containing DB + Lucene index + config. (Q1.3) |
| D7 | **Backup stores a version/schema marker** (app version / DB migration version) inside the archive, for restore-time compatibility checks. (Q1.4) |
| D8 | **Backup failures surface in a TaskRunner window** (progress/status window) showing the error to the user; any stale/partial backup data is cleaned up on failure. (Q1.5) — *assumption to confirm: no `TaskRunner` window exists yet in this codebase; likely a `Nameless.Windows` primitive not yet adopted here. Software-architect should confirm it's available/appropriate before this becomes a ticket.* |
| D9 | **A backup always covers all three artifacts together** (DB + Lucene index + config) — no partial/subset backup. (Q1.6) |
| D10 | **Cleanup on failure removes both the half-written zip and any temp/staging files** used while bundling. (Q1.7) |
| D11 | **Backups are kept as timestamped files side-by-side** in the fixed folder (D5) — no overwrite of a single file. (Q1.8) |
| D12 | **Restore file selection uses an Open-file dialog** browsing the fixed backup folder (D5/D11) — no in-app managed list UI. (Q2.1) |
| D13 | **Version/schema mismatch blocks restore with an error.** No migration attempt in this epic. (Q2.2) |
| D14 | **Restore prompts the user to relaunch the app** after a successful restore — not auto-relaunched. (Q2.3) |
| D15 | **Restore is atomic via a temp-location staging step**: restore to temp first, only swap into place on full success. (Q2.4) |
| D16 | **Restore shows a pre-restore warning/confirmation** that it overwrites current data, before proceeding. (Q2.5) |

## 3. Background (what exists today)

**IPX-0005 (#14, closed)** — stub ticket:
- Added disabled "coming soon" Backup/Restore cards (+ Check-for-update button) to the app
  configuration page. No command/click handler bound to any of them — deliberately dead UI.
- Explicitly deferred "real Backup/Restore/Check-for-update logic" to "a separate, later epic" — this one.

**Codebase today:**
- No backup/restore code exists in `src/` yet.
- Persisted state lives in three places: SQLite DB (`Data/AppDbContext.cs`), Lucene index
  (via `Nameless.Windows` search abstractions), app configuration (`IAppConfigurationManager`,
  bound from `appsettings.json`/user settings).

**Related epics / tickets (context only):** #11 (Setup & Initialization, closed) — parent of
the stub ticket, made no backup/restore semantic decisions.

**PO checkpoint 1 flags (not yet decisions — tracked as open questions below):**
- Restore needs *some* way to pick a backup file — even a plain file-open dialog counts as
  "Restore," but a managed list of past backups would really be "management" creeping back in.
- Trigger (manual button vs. any scheduling), destination (fixed folder vs. user-chosen path),
  version/schema compatibility across app releases, restart semantics on restore, and
  failure/corruption handling are all undecided.
- Check-for-update (also stubbed in IPX-0005) is a different concern — confirm it stays a
  separate future epic.

## 4. Part 1 — Backup creation

**Goal (from product owner):** let the user manually create a local backup of DB + Lucene
index + config into one artifact, so they don't lose data to a mistake, corruption, or
machine change.

**Confirmed:** D1, D2, D3, D4, D5, D6, D7, D8, D9.

### Working flow (from D4–D9)

```
[User clicks "Back up now" (D4)] -> [App bundles DB + Lucene index + config into one zip (D6, D9)]
   -> [Zip includes version/schema marker (D7)] -> [Written to fixed app-managed folder (D5)]
   -> on success: done
   -> on failure: TaskRunner window shows error, stale/partial backup data cleaned up (D8)
```

### Answered — Part 1

| # | Question | Answer |
|---|----------|--------|
| ~~Q1.1~~ | Trigger | Manual button only → D4 |
| ~~Q1.2~~ | Destination | Fixed app-managed folder → D5 |
| ~~Q1.3~~ | Archive shape | One bundled zip → D6 |
| ~~Q1.4~~ | Version marker | Yes, stored in backup → D7 |
| ~~Q1.5~~ | Failure handling | TaskRunner window error + cleanup of stale data → D8 |
| ~~Q1.6~~ | Subset backups | No, always all three together → D9 |

### Answered — Part 1 (follow-ups)

| # | Question | Answer |
|---|----------|--------|
| ~~Q1.7~~ | Stale-data cleanup scope | Half-written zip + temp/staging files → D10 |
| ~~Q1.8~~ | Folder naming convention | Timestamped files side-by-side, no overwrite → D11 |

**Part 1: closed.**

## 5. Part 2 — Restore flow

**Goal (from product owner):** let the user restore DB + Lucene index + config from a
previously created backup, recovering from data loss or corruption.

**Confirmed:** D1, D2, D3, D12, D13, D14, D15, D16.

### Working flow (from D12–D16)

```
[User clicks "Restore..." -> Open-file dialog on fixed backup folder (D12)]
   -> [Pre-restore warning: this overwrites current data (D16)] -> user confirms
   -> [Restore to temp location (D15)]
      -> version/schema marker mismatch? -> block with error, abort, nothing swapped (D13)
      -> match? -> swap temp into place atomically (D15)
   -> [Prompt user to relaunch app (D14)]
```

### Answered — Part 2

| # | Question | Answer |
|---|----------|--------|
| ~~Q2.1~~ | Backup selection | Open-file dialog on fixed folder → D12 |
| ~~Q2.2~~ | Version/schema mismatch | Block with error, no migration → D13 |
| ~~Q2.3~~ | Restart semantics | Prompt user to relaunch → D14 |
| ~~Q2.4~~ | Failure/safety net | Temp-staging, atomic swap on success → D15 |
| ~~Q2.5~~ | Pre-restore confirmation | Required, warns of overwrite → D16 |

**Part 2: closed.**

## 6. Dependencies

| Dependency | Owner | Source |
|------------|-------|--------|
| Check-for-update feature | Separate future epic — this epic doesn't touch it | PO checkpoint 1 flag |
| TaskRunner window availability | Confirm with software-architect that it exists / is adoptable from `Nameless.Windows` | D8 assumption |

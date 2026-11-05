# Backup & Restore

**Type:** Epic · **Key:** [IPX-0007](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23) · **Project/Repo:** GitHub `marcoaoteixeira/InfoPhoenix` · **Team:** Solo (marcoaoteixeira)
**Source:** brainstorming doc `Backup-Restore-Epic.md` (decision refs D1–D16)

---

## Summary

Let the InfoPhoenix user manually create a local backup of their data and restore from one,
so a mistake, corruption, or machine change doesn't mean losing their data. Desktop-only,
solo-dev scope — no server, account, or cloud sync.

This epic:
1. **Backup creation** — bundle the SQLite DB, Lucene index, and app configuration into one
   timestamped zip in a fixed app-managed folder, triggered by a manual "Back up now" action.
2. **Restore flow** — pick a backup zip via an Open-file dialog, confirm the overwrite warning,
   stage the restore in a temp location, and atomically swap it into place on success; the
   user relaunches the app afterward.

## Background

- **Current state:** IPX-0005 (#14, closed) added disabled "coming soon" Backup/Restore cards
  (and a disabled Check-for-update button) to the app configuration page, with no logic bound —
  explicitly deferred to this epic.
- **Existing building blocks:** app configuration page (IPX-0004), `AppDbContext` (SQLite),
  Lucene search abstractions and `IAppConfigurationManager` from `Nameless.Windows`.
- **Not yet confirmed to exist:** a `TaskRunner`-style progress/error window — see Dependencies.

## Flow

```
BACKUP
[User clicks "Back up now"] -> [Bundle DB + Lucene index + config + version marker into one zip]
   -> [Write to fixed app-managed folder, timestamped filename]
   -> success: done
   -> failure: TaskRunner window shows error; half-written zip + temp/staging files cleaned up

RESTORE
[User clicks "Restore..."] -> [Open-file dialog on fixed backup folder] -> [pick a backup zip]
   -> [Warning: this overwrites current data] -> user confirms
   -> [Restore to temp location]
      -> version/schema marker mismatch -> block with error, abort, nothing swapped
      -> match -> atomically swap temp into place
   -> [Prompt user to relaunch the app]
```

## Scope

### 1. Backup creation
- **Trigger:** manual "Back up now" action only (the existing stub card) — no scheduling.
- **Contents:** SQLite DB + Lucene index + app configuration, always bundled together — no
  partial/subset backups.
- **Format:** one zip archive, including an embedded version/schema marker for later
  compatibility checks.
- **Destination:** fixed app-managed folder, one timestamped file per backup (no overwrite).
- **Failure handling:** errors surface in a TaskRunner window; any half-written zip and
  temp/staging files used while bundling are cleaned up.

### 2. Restore flow
- **Selection:** Open-file dialog browsing the fixed backup folder — no in-app managed list.
- **Safety:** pre-restore confirmation warning that current data will be overwritten.
- **Compatibility:** if the backup's version/schema marker doesn't match the current app,
  block with an error — no migration attempt.
- **Atomicity:** restore to a temp location first; only swap into place on full success.
- **Completion:** prompt the user to relaunch the app — no auto-relaunch, no hot-swap while running.

## Out of scope
- Backup browsing/management UI (list, delete, retention policy).
- Scheduled/automatic backups.
- Cloud or network backup destinations.
- Schema migration on version mismatch during restore.
- Check-for-update — separate future epic, untouched here.

## Dependencies
| Dependency | Owner |
|------------|-------|
| TaskRunner-style progress/error window | Confirm with software-architect it exists / is adoptable from `Nameless.Windows` before ticketing any failure-surfacing AC — backup failure (D8), restore version-mismatch error, and mid-restore failure all surface this way |

## To settle at refinement
- Exact fixed folder path/location convention (e.g. under app data dir) and timestamp filename format.
- Exact version/schema marker format and what "mismatch" compares (app version, DB migration version, or both).
- Wording/placement of the pre-restore confirmation dialog and the post-restore relaunch prompt.

## Candidate tickets

| Area | Ticket | Acceptance criteria |
|------|--------|---------------------|
| Backup | Wire "Back up now" action to create a backup | Clicking the (now-enabled) Backup card bundles DB + Lucene index + config + version marker into one timestamped zip in the fixed folder; UI shows a concrete success indicator (e.g. status message naming the created file). |
| Backup | Backup failure handling | On any failure (disk full, unwritable target, etc.) a TaskRunner window shows the error; half-written zip and temp/staging files are removed; no partial backup is left behind. (Depends on: Backup creation ticket.) |
| Restore | Wire "Restore" action to pick a backup | Clicking the (now-enabled) Restore card opens a file dialog on the fixed backup folder; selecting a zip shows the overwrite warning before proceeding. (Depends on: Backup creation ticket, so there's a zip format to open.) |
| Restore | Restore version/compatibility check | If the selected backup's version/schema marker doesn't match the running app, restore is blocked with a clear error (via TaskRunner window, pending that dependency) and no files are changed. (Depends on: Restore selection ticket.) |
| Restore | Atomic restore + relaunch prompt | On confirmed, compatible restore: DB + Lucene index + config are staged to a temp location, then atomically swapped in only on full success; user is then prompted to relaunch the app. Any staging failure surfaces via TaskRunner window and leaves prior state untouched. (Depends on: Restore selection + version-check tickets.) |
| Quality | Tests for Backup/Restore happy path + failure/mismatch cases | Covers: successful backup; backup failure cleanup; successful restore; version-mismatch unit check AND an end-to-end case where a real mismatched marker blocks restore; restore failure leaves prior state intact (temp staging never partially swapped). (Depends on: all above tickets.) |

## References
- Linked docs: none
- Tracker: IPX-0005 (#14) — stub ticket this epic completes

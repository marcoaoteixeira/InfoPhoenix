# IPX-0007 Backup & Restore — Child Tickets

Parent epic: [IPX-0007](https://github.com/marcoaoteixeira/InfoPhoenix/issues/23) (#23)

PO breakdown merged the original 6 candidates into 5 tickets + 1 spike (solo-dev sizing —
avoids splitting tightly-coupled implementation steps into separately-shipped halves).

| Order | Key | Title | Status |
|---|---|---|---|
| 0 | [IPX-0008](https://github.com/marcoaoteixeira/InfoPhoenix/issues/24) | Confirm progress/error-surfacing mechanism for Backup/Restore (Spike) | Backlog |
| 1 | [IPX-0009](https://github.com/marcoaoteixeira/InfoPhoenix/issues/25) | Create a local backup (happy path + failure handling) | Backlog |
| 2 | [IPX-0010](https://github.com/marcoaoteixeira/InfoPhoenix/issues/26) | Select a backup and confirm overwrite | Backlog |
| 3 | [IPX-0011](https://github.com/marcoaoteixeira/InfoPhoenix/issues/27) | Validate compatibility and perform atomic restore | Backlog |
| 4 | [IPX-0012](https://github.com/marcoaoteixeira/InfoPhoenix/issues/28) | Tests: Backup/Restore happy path, failure and mismatch cases | Backlog |

All 5 attached as GitHub sub-issues of epic #23, and all 6 (epic + children) are on the
InfoPhoenix project board (project #2), Status = Backlog.

## 0. IPX-0008 — Spike: Confirm progress/error-surfacing mechanism

**Value:** De-risks 3 later tickets before their AC are written against an unconfirmed
primitive; prevents rework.

**Question to answer:** Does `Nameless.Windows` (or the `Nameless.WinApp` reference sample)
already expose a reusable progress/error/status window suitable for a multi-second
bundling or restore operation? If yes, how is it invoked/wired. If no, what's the minimal
substitute (e.g. a simple modal result dialog bound via MVVM) that keeps this epic's scope
from quietly growing into "build a progress-window framework."

**Time-box:** half a day. **Output:** a short note naming the chosen mechanism and how the
Backup/Restore tickets should call it — no production code required beyond a throwaway proof
if needed.

**Dependencies:** none. Blocks IPX-0009, IPX-0011 (and indirectly IPX-0012's failure-path tests).

## 1. IPX-0009 — Create a local backup (happy path + failure handling)

**Story:** As an InfoPhoenix user, I want to click "Back up now" and get a single backup file
of my DB, search index and settings, so a mistake or machine change doesn't cost me my data.

**Scope:** Enable the existing "Back up now" stub card; bundle SQLite DB + Lucene index
directory + app configuration + an embedded version/schema marker into one zip, written to a
fixed app-managed folder with a timestamped filename (never overwrites a prior backup); on
success show a concrete status indicator naming the created file; on any failure (disk full,
unwritable target, etc.) surface the error via the mechanism confirmed in IPX-0008, and
remove any half-written zip and temp/staging files — no partial backup left behind.

**Out of scope:** Restore, backup browsing/management UI, scheduling.

**Acceptance criteria:**
- Given the configuration page, when the user clicks "Back up now," then a zip containing
  DB + Lucene index + config + version marker is written to the fixed folder with a
  timestamped name, and a success message names the created file.
- Given a backup in progress, when writing fails for any reason, then the error is shown via
  the confirmed progress/error mechanism, and no half-written zip or temp/staging file remains
  afterward.
- Given two consecutive backups, when both succeed, then two distinct timestamped files exist
  — the first is not overwritten.

**Assumptions (settle at kickoff):** fixed folder path/location convention and timestamp
format; version/schema marker format (app version vs. DB migration version vs. both) — the
*comparison* logic lives in IPX-0011, but the marker is written here, so format must be
decided now.

**Depends on:** IPX-0008.

## 2. IPX-0010 — Select a backup and confirm overwrite

**Story:** As an InfoPhoenix user, I want to pick a backup file and be warned before
restoring, so I don't accidentally overwrite my current data without realizing it.

**Scope:** Enable the existing "Restore..." stub card; clicking it opens an Open-file dialog
browsing the fixed backup folder; after a zip is picked, show a warning that current data
will be overwritten and require explicit confirmation before proceeding. This ticket stops at
confirmation — it does not touch any files.

**Out of scope:** Actually restoring data, version-mismatch checking (IPX-0011).

**Acceptance criteria:**
- Given the configuration page, when the user clicks "Restore...", then an Open-file dialog
  opens, scoped to the fixed backup folder.
- Given a zip file is selected, when the dialog closes, then a warning dialog states current
  data will be overwritten and requires explicit confirmation to continue.
- Given the user cancels the file dialog or declines the overwrite warning, then no restore
  proceeds and no state changes.

**Depends on:** IPX-0009 (needs a real zip format to open against).

## 3. IPX-0011 — Validate compatibility and perform atomic restore

**Story:** As an InfoPhoenix user who has confirmed a restore, I want the app to safely check
the backup is compatible and swap it in without risking my current data, so a bad or
incompatible backup can't leave the app broken.

**Scope:** After the user confirms (IPX-0010), restore DB + Lucene index + config from the
chosen zip to a temp location; compare the embedded version/schema marker against the
running app's — on mismatch, block with a clear error (via the confirmed mechanism) and leave
current state untouched; on match, atomically swap the staged temp data into place only on
full success; after a successful swap, prompt the user to relaunch the app (no auto-relaunch).
Any staging failure surfaces via the same error mechanism and leaves prior state untouched.

**Out of scope:** Schema migration on mismatch (explicitly out of epic scope); auto-relaunch.

**Acceptance criteria:**
- Given a confirmed restore with a mismatched version/schema marker, when restore runs, then
  it's blocked with a clear error and no existing files are changed.
- Given a confirmed restore with a matching marker, when staging to temp succeeds fully, then
  the temp data is atomically swapped into place and the user is prompted to relaunch.
- Given a confirmed restore with a matching marker, when staging to temp fails partway (e.g.
  disk error), then the error surfaces via the confirmed mechanism and the prior DB/index/config
  remain exactly as they were — no partial swap.

**Assumptions (settle at kickoff):** version/schema marker comparison rule (app version only,
DB migration version only, or both). This is the highest-risk ticket in the epic — a short
design note on the swap mechanism (rename-based vs. copy-then-delete) is worth doing first.

**Depends on:** IPX-0010, IPX-0008.

## 4. IPX-0012 — Tests: Backup/Restore happy path, failure and mismatch cases

**Scope:** Automated coverage for: successful backup; backup failure leaves no partial files;
successful restore with matching marker; version-mismatch blocks restore (both a focused unit
check and an end-to-end case using a real mismatched marker); restore failure during staging
leaves prior state fully intact (no partial swap).

**Out of scope:** UI/manual exploratory testing beyond what's needed to drive the above
through the app's seams.

**Acceptance criteria:**
- Given the scenarios above, when the test suite runs, then each scenario has an automated
  test, and all pass.
- Given a simulated staging failure mid-restore, when the test runs, then it asserts DB +
  Lucene index + config files are byte-for-byte unchanged from before the attempt.

**Depends on:** IPX-0009, IPX-0010, IPX-0011.

## IPX-0005 (#14, closed) — no change

Confirmed: its scope (disabled stub cards, no logic) is already fully satisfied as written;
real logic was explicitly deferred to this epic. No reopen, no edit.

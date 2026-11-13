# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- A modal splash screen shown on startup, before the main window, with live
  bootstrap progress (current step name/message and an indeterminate
  progress bar), the application name/version, closing automatically once
  bootstrap finishes. ([IPX-0003](https://github.com/marcoaoteixeira/InfoPhoenix/issues/12))
- An application configuration page (nav menu, bottom placement) with
  Theme, Language and Confirm-before-exit settings that apply immediately
  and save as soon as they change, plus buttons to open the app data
  directory and the current log file, and a read-only app version display.
  Stored theme (including System), language and confirm-before-exit now
  take effect on the next launch and on window close.
  ([IPX-0004](https://github.com/marcoaoteixeira/InfoPhoenix/issues/13))
- Backup and Restore cards and a Check-for-update button on the
  application configuration page, each shown disabled with a
  "coming soon" badge and tooltip, as placeholders for upcoming features.
  ([IPX-0005](https://github.com/marcoaoteixeira/InfoPhoenix/issues/14))
- Recovery from invalid or unreadable app configuration: opening the
  application configuration page now detects an invalid or unreadable
  Theme, Language or Confirm-before-exit setting, resets every setting
  back to its default, applies the defaults immediately and shows a
  failure snackbar notification, instead of leaving the page stuck or
  blank. ([IPX-0006](https://github.com/marcoaoteixeira/InfoPhoenix/issues/15))
- A working Backup button on the application configuration page: creates a
  local backup archive (settings, database and search index) under
  `backups\`, reporting progress and any missing-artifact warnings through
  snackbar notifications, with the application configuration page's Backup
  card enabled accordingly.
  ([IPX-0009](https://github.com/marcoaoteixeira/InfoPhoenix/issues/25))
- A working Restore button on the application configuration page: opens an
  Open-file dialog scoped to the backup folder, then a warning dialog
  requiring explicit confirmation before overwriting current data.
  Cancelling the dialog or declining the warning leaves everything
  untouched; actually restoring the selected backup is a later ticket.
  ([IPX-0010](https://github.com/marcoaoteixeira/InfoPhoenix/issues/26))
- Restore now validates the selected backup (archive integrity, manifest,
  app/database compatibility) and extracts it to a temporary folder
  before touching anything live. Only once everything checks out does it
  atomically swap the settings, database and search index into place
  through same-volume renames that are fully reversed if any step of the
  swap fails, leaving the previous data untouched either way.
  ([IPX-0011](https://github.com/marcoaoteixeira/InfoPhoenix/issues/27))

### Changed

- Migrated `ITranslator` usage from the old resx-backed translator to
  `Nameless.Windows`'s JSON-file-backed `JsonTranslator`. Nine classes
  (view models, request handlers, a validator and a migration step) now
  depend on `ITranslator<T>` instead of the non-generic `ITranslator`, and
  their localized strings — plus a shared "UI" section used by three XAML
  views — now live in `Resources/loc/default.json` and
  `Resources/loc/pt-BR.json`. As an interim side effect, editing
  `Resources/l10n/Localization.resx`/`.pt-BR.resx` no longer has any
  runtime effect except on the three page navigation titles that still
  read from it; the rest of the UI ignores those files until the
  remaining `*.xaml.cs` code-behind is migrated in a follow-up ticket.
  ([IPX-0015](https://github.com/marcoaoteixeira/InfoPhoenix/issues/39))

### Fixed

- `Translator` configuration in `appsettings.json` pointed only at a dead
  `ResourceFullName` key, which made `JsonTranslator`'s file provider throw
  `MissingConfigurationException` as soon as anything resolved
  `ITranslator`, crashing startup. Replaced it with a `Translator:RootPath`
  pointing at the new `Resources\loc` JSON files.
  ([IPX-0015](https://github.com/marcoaoteixeira/InfoPhoenix/issues/39))
- The main window now actually subscribes to snackbar notifications
  published through the event publisher, so notifications (including
  IPX-0004's "missing log file" warning) are shown instead of silently
  never appearing.
  ([IPX-0006](https://github.com/marcoaoteixeira/InfoPhoenix/issues/15))
- The main window's startup theme read, its closing handler's
  confirm-before-exit read, and the bootstrap step that applies the UI
  language no longer crash the application when the stored app
  configuration cannot be read; each falls back to its default value and
  logs a warning instead.
  ([IPX-0006](https://github.com/marcoaoteixeira/InfoPhoenix/issues/15))

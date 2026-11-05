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

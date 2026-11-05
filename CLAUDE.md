# CLAUDE.md

Guidance for Claude Code (and any other AI agent) working in this repository.

## Project

InfoPhoenix is a from-scratch rewrite of the legacy
[InfoFenix](https://github.com/marcoaoteixeira/InfoFenix) (.NET Framework 4.5)
on a modern .NET runtime. Windows-only desktop app.

Main app project: `Nameless.InfoPhoenix.Client`
Test project (mirrors main project): `Nameless.InfoPhoenix.Client.Tests`

See `README.md` for release info, contribution and issue-filing process.

## Dev-Team Detected Facts

- Stack: backend `dotnet` (marker: `src/Nameless.InfoPhoenix.Client/Nameless.InfoPhoenix.Client.csproj`), no separate frontend (WPF UI is part of the dotnet project) — detected 2026-11-04 by stack-detection. Delete this line to force re-detection.
- Tracker: `github-issues` (evidence: `gh auth status` + `gh project list --owner marcoaoteixeira` succeed; no github MCP connected, operations go through `gh` CLI directly) — detected 2026-11-04 by tracker-detection. Delete this line to force re-detection.

## Stack

- C# — latest stable language version, most modern idioms available
  (primary constructors, collection expressions, pattern matching, etc.)
- .NET 10 (`net10.0-windows`), WPF, `win-x64`
- WPF (XAML) for UI
- [WPF-UI](https://github.com/lepoco/wpfui) for modern/fluent chrome and controls
- CommunityToolkit.Mvvm for MVVM (ObservableObject, `[ObservableProperty]`,
  `[RelayCommand]`, messenger where needed)
- Nameless.Windows (private package, see below) for app hosting, DI
  bootstrap, app configuration, localization, Lucene search integration, etc.
- SQLite as the relational store, accessed via EF Core
  (`Microsoft.EntityFrameworkCore.Sqlite`)
- Lucene for full-text search — do **not** add a raw `Lucene.Net` package
  reference; use the search abstractions already provided by
  `Nameless.Windows`
- xUnit for tests

## Reference implementation

This project is bootstrapped from, and should follow the same conventions
as, the `Nameless.WinApp` sample in the Nameless repo:
https://github.com/marcoaoteixeira/Nameless/tree/main/samples/windows/Nameless.WinApp

When unsure how to wire something (hosting, DI, a new feature, navigation,
localization, config), look at how the sample does it first rather than
inventing a new pattern. Observed conventions from that sample:

- `App.xaml.cs` builds a `WinHost` via `WinHostFactory.CreateDefault(...)`
  and drives startup/shutdown through `OnStart`/`OnStop` hooks — don't
  hand-roll DI container setup in code-behind.
- `Infrastructure/` — cross-cutting host features (each implementing the
  host's feature-configuration pattern, e.g. `ApplicationHostFeature`,
  `SystemUpdateHostFeature`) and small supporting services.
- `Features/` — vertical slices, one folder per use case
  (e.g. `CheckSystemUpdate/`, `PerformApplicationBackup/`), not grouped by
  technical layer.
- `Data/` — EF Core `DbContext` and configs.
- `Views/` and `ViewModels/` — mirrored subfolders, split into `Pages/` and
  `Windows/`.
- `Constants.cs` — single static class, nested by concern
  (`Labels`, `Database`, `Logging`, etc.), for magic strings shared across
  the project.
- `appsettings.json` copied to output, bound into strongly-typed options
  (e.g. a `Lucene` section with `DirectoryRelativePath`).
- Serilog for logging (console + file sinks), configured from
  `appsettings.json`.

## Repository layout

```
src/Nameless.InfoPhoenix.Client/
tests/Nameless.InfoPhoenix.Client.Tests/
```

`Directory.Build.props` at the repo root detects "is this a src project" by
matching a `/src/` (or `\src\`) segment in the project path, and "is this a
test project" by the `.Tests.csproj` suffix — keep new projects under
`src/` or `tests/` accordingly so those properties (code analysis,
`TreatWarningsAsErrors`, xUnit usings, etc.) apply correctly.

## Build settings (from `Directory.Build.props`)

- `Nullable` enabled, `ImplicitUsings` enabled, `LangVersion` latest
- `TreatWarningsAsErrors` is **true** — don't suppress a warning with
  `#pragma` or `NoWarn` without a good reason; fix the actual issue
- Code analysis + analyzers run during build for `src/` projects
- Test projects get `OutputType=Exe`, no doc-file generation, and the
  `Xunit` global using

## Package versions

Central Package Management is in use: all `PackageVersion`s live in
`Directory.Packages.props` at the repo root. Project `.csproj` files only
have `<PackageReference Include="..." />` with no `Version` attribute —
add new/updated versions to `Directory.Packages.props`, not the project
file.

## Package registry

Private Nameless packages (`Nameless.Windows`, etc.) come from our GitHub
Packages NuGet feed. `nuget.config` at the repo root is gitignored — it
holds the feed URL and credentials locally, never commit it or its
contents (including any token) anywhere else (code, commit messages,
issues).

Because the project uses Central Package Management with two package
sources (`nuget.org` and `github.com`), the local `nuget.config` must
include package source mapping (`Nameless.*` → `github.com`, everything
else → `nuget.org`) or restore fails with `NU1507`.

## Source control / workflow

- GitHub for version control, GitHub Projects for issue tracking.
- Project board columns: `Backlog` → `In Progress` → `In Review` → `Done`.
- Open issues for bugs/features before starting work when practical; link
  PRs to the issue they close.
- Issue numbering: project prefix `IPX` + a 4-digit zero-padded sequence,
  e.g. `IPX-0001`, `IPX-0002`. The issue title is `IPX-00NN-ShortTitle`.
- Branch names carry the issue id: `feature/IPX-00NN-ShortTitle` (or
  `fix/...`, `chore/...` matching the Conventional Commit type).
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/)
  (`type(scope): subject`, e.g. `feat(client): add main window shell`).

## Conventions

- Don't introduce abstractions or config beyond what the current feature
  needs — follow the vertical-slice/feature-folder style already used by
  the sample instead of premature layering.
- Prefer the DI/hosting primitives already exposed by `Nameless.Windows`
  over new ad-hoc singletons or service locators.
- Keep EF Core migrations and the `AppDbContext` under `Data/`.
- Mirror `ViewModels/` structure under `Views/` (same subfolder, same
  name minus suffix).

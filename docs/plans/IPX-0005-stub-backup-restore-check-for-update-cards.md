# IPX-0005: Stub Backup, Restore and Check-for-update cards

- Ticket: [IPX-0005-StubBackupRestoreCheckForUpdateCards (#14)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/14)
- Epic: [#11 Setup & Initialization](https://github.com/marcoaoteixeira/InfoPhoenix/issues/11)
- Builds on: IPX-0004 (`AppConfigurationPage`, merged to `main`)
- Stack: .NET 10 / WPF (`dotnet`), single project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. The project has no separate frontend; the WPF UI is part of the same .NET project.
- UX spec: **not needed**. The page, the card layout and the target design (the sample's working cards) already exist. This ticket adds one indicator (badge + tooltip) to an established pattern.
- ADR: **none**. This is a XAML-only addition that follows the existing page pattern. It changes no framework, external integration or data model.

## 1. Context

**InfoPhoenix today (this worktree, `Views/Pages/AppConfigurationPage.xaml`):** a `StackPanel` of `ui:CardControl`s in this order: Version, Language, Theme, Exit behavior, App directory, Application log. Every card uses the same header (a two-row `Grid` with a `BodyStrong` title and a message, all through `{loc:Translator Key}`), and the action buttons are `ui:Button Appearance="Primary"` bound by `ElementName=AppConfigurationPageRoot, Path=ViewModel.<X>Command`.

**Sample (`Nameless.WinApp`, `Views/Pages/AppConfigurationPage.xaml` on `main`), the eventual target:**

| Feature | Where it lives in the sample | Icon | Button |
|---|---|---|---|
| Check for update | A button **on the Version card** (not its own card) | card: app image | `VersionCard_Button_Caption`, `Globe32`, `Primary`, `PerformSystemUpdateCommand` |
| Backup | Own card, after Language | `DatabaseMultiple20` | `ApplicationBackupCard_Button_Caption`, `DatabaseArrowDown20`, `Success`, `PerformApplicationBackupCommand` |
| Restore | Own card, after Backup | `DatabaseLightning20` | `ApplicationRestoreCard_Button_Caption`, `DatabaseArrowUp20`, `Success`, `PerformApplicationRestoreCommand` |

The sample's card order is Version, Language, **Backup**, **Restore**, Theme, Exit behavior, App directory, Application log.

**Package facts this design depends on (WPF-UI 4.3.0, resolved through `Nameless.Windows`):**
- `ui:CardControl` derives from `ButtonBase`. It has its own `Command`/`Click` and hover/pressed visuals, so the card itself is clickable, not only its inner button.
- `ui:Badge` exists (`Content`, `Appearance`), so the "coming soon" badge needs no custom control.
- WPF does not show a `ToolTip` on a disabled element unless `ToolTipService.ShowOnDisabled="True"` is set.
- Setting `IsEnabled="False"` on a parent disables its children (`IsEnabled` is coerced through the tree). Disabled elements receive no mouse or keyboard input and are skipped by Tab.

## 2. Approach

### 2.1 Decisions

- **D1: Layout follows the sample.** Backup and Restore become their own cards, inserted between Language and Theme. Check-for-update becomes a button on the **existing Version card**, not a fourth card. The later epic then only has to add the command bindings and remove the stubs, with no layout reshuffle. *Alternative:* a standalone "Check for update" card, which matches the ticket wording "three cards" most literally but would be moved onto the Version card when the real feature is ported. See assumption A1. The choice is XAML-only and cheap to flip.
- **D2: Nothing executes because nothing is bound.** The stubs bind **no `Command`** and handle **no `Click`**, neither on the `CardControl` nor on the inner `ui:Button`, and the view model gets **no new members**. *Alternative rejected:* stub `[RelayCommand]`s with `CanExecute => false`. That adds dead view-model surface, risks becoming the "silent no-op" the AC forbids if `CanExecute` is ever flipped, and the later epic would rewrite them anyway. With no binding there is nothing to execute, which satisfies AC2 by construction.
- **D3: What "disabled" covers.**
  - Backup and Restore: the **whole card** gets `IsEnabled="False"`. Because the card is a `ButtonBase`, an enabled card would still show hover/press affordance. Disabling it also disables the inner button.
  - Check-for-update: **only the button** gets `IsEnabled="False"`. The Version card stays enabled because its version text must stay readable (IPX-0004 AC4), and it has no command of its own.
- **D4: The indicator is a visible badge plus a tooltip.**
  - The **badge** is a `ui:Badge` reading "Coming soon", placed next to the card title. It is always visible and also reaches keyboard and screen-reader users, who cannot focus a disabled control and so never see its tooltip.
  - The **tooltip** reads "Not available yet. This feature is coming in a future version." It sits on each disabled element (the Backup card, the Restore card and the Check-for-update button) with `ToolTipService.ShowOnDisabled="True"`. Without that attribute the tooltip never appears, which is the main trap in this ticket.
  - The badge uses `Appearance="Info"`. Do not use `Danger`/`Caution`, which would suggest a fault ("without implying they're broken", ticket Value).

### 2.2 Files (all under `src/Nameless.InfoPhoenix.Client/`)

| File | Change |
|---|---|
| `Views/Pages/AppConfigurationPage.xaml` | Add the Backup and Restore cards and the Check-for-update button plus badge on the Version card |
| `Resources/l10n/Localization.resx` | Add the new keys (English) |
| `Resources/l10n/Localization.pt-BR.resx` | Add the new keys (pt-BR) |
| `Resources/l10n/Localization.Designer.cs` | Regenerate, or hand-add the matching properties in the existing style |
| `ViewModels/Pages/AppConfigurationPageViewModel.cs` | **No change** (D2) |

### 2.3 Page (binding for the developer)

- **Version card:**
  - Header: put the title and the badge on one line (e.g. title and `ui:Badge` in a horizontal `StackPanel` in row 0). Keep the existing `AppVersionTextBlock` in row 1 unchanged.
  - Card content: a `ui:Button` with `x:Name="CheckForUpdateButton"`, `Appearance="Primary"`, `Icon="{ui:SymbolIcon Globe32}"`, `Padding="30 8"`, `Content="{loc:Translator AppConfigurationPage_VersionCard_Button_Caption}"`, `IsEnabled="False"`, `ToolTip="{loc:Translator AppConfigurationPage_ComingSoon_Tooltip}"`, `ToolTipService.ShowOnDisabled="True"`. **No `Command`.** Leave out `Cursor="Hand"`, because a hand cursor promises a click.
- **Backup card** (`x:Name="ApplicationBackupCard"`), inserted after the Language card:
  - Card: `Icon="{ui:SymbolIcon DatabaseMultiple20}"`, `Margin="0 0 0 10"`, `IsEnabled="False"`, the same tooltip attributes as the Check-for-update button, **no `Command`**.
  - Header: the standard two-row header. Row 0 holds the title and the badge, row 1 the message.
  - Content: a `ui:Button` with `Appearance="Success"`, `Icon="{ui:SymbolIcon DatabaseArrowDown20}"`, `Padding="30 8"`, the caption key, **no `Command`** and no `Cursor="Hand"`.
- **Restore card** (`x:Name="ApplicationRestoreCard"`), inserted after Backup: the same as Backup, with `DatabaseLightning20` as the card icon and `DatabaseArrowUp20` as the button icon.
- **Badge** (all three places): `<ui:Badge Appearance="Info" Content="{loc:Translator AppConfigurationPage_ComingSoon_Badge}" Margin="8 0 0 0" VerticalAlignment="Center" />`.

### 2.4 Localization keys (both `.resx` files; values are a suggestion, wording is the developer's call)

| Key | en | pt-BR |
|---|---|---|
| `AppConfigurationPage_VersionCard_Button_Caption` | Check for update | Verificar atualização |
| `AppConfigurationPage_ApplicationBackupCard_Title` | Backup | Backup |
| `AppConfigurationPage_ApplicationBackupCard_Message` | Create a backup of the application data. | Cria uma cópia de segurança dos dados da aplicação. |
| `AppConfigurationPage_ApplicationBackupCard_Button_Caption` | Create backup | Criar backup |
| `AppConfigurationPage_ApplicationRestoreCard_Title` | Restore | Restauração |
| `AppConfigurationPage_ApplicationRestoreCard_Message` | Restore the application data from a backup. | Restaura os dados da aplicação a partir de um backup. |
| `AppConfigurationPage_ApplicationRestoreCard_Button_Caption` | Restore backup | Restaurar backup |
| `AppConfigurationPage_ComingSoon_Badge` | Coming soon | Em breve |
| `AppConfigurationPage_ComingSoon_Tooltip` | Not available yet. This feature is coming in a future version. | Ainda não disponível. Este recurso virá em uma versão futura. |

Watch-out: `LocalizationFitnessTests` asserts that the pt-BR value **differs** from the neutral one. "Backup" / "Backup" for `ApplicationBackupCard_Title` would fail it, so use a distinct pt-BR title (e.g. "Cópia de segurança") or another distinct wording. Don't weaken the test.

## 3. Contract

None. There is no API, event or view-model surface change. The only "contract" is the set of localization keys in 2.4.

## 4. Quality attributes and fitness functions

- **Correctness of "does nothing, visibly" (AC2):** **F1** is a component test (2.6 / T2) asserting that every stub element is disabled and has `Command == null`. It fails as soon as someone binds a command without removing the stub state.
- **Localizability:** **F2** extends `LocalizationFitnessTests.Keys` with the nine new keys. It catches a missing or untranslated key before it shows up at runtime as a raw `[Key]` placeholder.
- **Accessibility:** the badge is visible text, so the indicator does not depend on hover. Check contrast in the Light, Dark and HighContrast themes manually (no cheap automated check).
- **Evolvability:** following the sample's layout and key names (D1) means the later epic adds bindings without reshaping the page.

## 5. Security notes

No new entry point, data flow, input, storage or logging. Nothing to threat-model.

## 6. Risks

- **R1:** a tooltip that never appears because `ToolTipService.ShowOnDisabled` is missing. Covered by T2 and the manual check.
- **R2:** a disabled `CardControl` might grey out the badge until it is hard to read, especially in HighContrast. If so, keep the badge readable (for example, take Backup/Restore off whole-card `IsEnabled="False"` and disable only the button, accepting the card's hover visual), and say so in the PR.
- **R3:** `Localization.Designer.cs` falls out of sync with the `.resx` if it isn't regenerated. That is harmless at runtime (`{loc:Translator}` uses the `ResourceManager`), but keep it consistent.
- **R4:** the identical "Backup" pt-BR value trips F2 (see 2.4).

## 7. Work split and tasks (backend-developer-dotnet), in build order

1. Add the nine localization keys to `Localization.resx` and `Localization.pt-BR.resx`, and update `Localization.Designer.cs` (AC1).
2. Version card: add the badge in the header and the disabled, command-less Check-for-update button with its tooltip (AC1, AC2).
3. Add the Backup and Restore cards between Language and Theme: disabled, command-less, with badge and tooltip (AC1, AC2).
4. Tests: extend `LocalizationFitnessTests.Keys` (F2), and add the component test T2 to `tests/.../Views/Pages/AppConfigurationPageTests.cs` (AC1, AC2).
5. Manual check: hover each stub to see the tooltip, click each one and confirm nothing happens and nothing is logged, Tab skips them, and the badge is readable in all four themes and both languages.

## 8. Test strategy

| AC | Test | Level |
|---|---|---|
| AC1 | **T1:** F2, nine keys resolve to non-empty, distinct en/pt-BR values. | Unit |
| AC1, AC2 | **T2** (STA, same setup as `AppVersionTextBlock_IsATextBlockNotAnEditableControl`: `TranslatorExtension.SetTranslator(new FakeTranslator())`, construct the page). For `ApplicationBackupCard`, `ApplicationRestoreCard` and `CheckForUpdateButton` found through `FindName`, assert: `IsEnabled == false`, `Command == null`, `ToolTip != null`, `ToolTipService.GetShowOnDisabled(element) == true`. For both cards, also find the inner `ui:Button` (give it an `x:Name`, e.g. `ApplicationBackupButton` / `ApplicationRestoreButton`) and assert `Command == null` and `IsEnabled == false` (inherited). Asserting that each badge is present needs an `x:Name` on it too, which is optional. | Component |
| AC2 | Manual: clicking or pressing keys on each stub causes no action, no exception and no log entry. The version text still renders and the other cards still work. | Manual |

**Failure modes for QA to probe:** tooltip on hover over a **disabled** card and button. Language switched live while on the page (the badge and tooltip text update). HighContrast readability of the badge on a disabled card (R2). Double-click and Enter/Space on the cards. Tab order skips the stubs, and Narrator reads the badge text.

## 9. Open decisions / assumptions

None blocking.

- **A1 (for the ticket owner to confirm, cheap to flip):** "Check-for-update card" is read as the **Check-for-update action on the Version card**, which is where the sample puts it (D1). If the owner wants a standalone fourth card, the developer adds one card in the Backup/Restore style instead of the button on the Version card. The only change is to XAML and T2.

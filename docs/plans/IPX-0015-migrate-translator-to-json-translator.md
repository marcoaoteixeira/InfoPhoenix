# IPX-0015: Migrate the translator to JsonTranslator and ITranslator<T>

- Ticket: [IPX-0015 (#39)](https://github.com/marcoaoteixeira/InfoPhoenix/issues/39).
- Stack: .NET 10 / WPF (`dotnet`), one project `src/Nameless.InfoPhoenix.Client`. Agent pair: `backend-developer-dotnet` / `backend-reviewer-dotnet`.
- Work split: **BACKEND-ONLY**. This is a desktop app with one codebase and no client/server API, so there is no contract section.
- UX spec: **not needed**. No flow or screen changes. Every visible string keeps its current text.
- ADR: **none**. The decision to move from resx to JSON was made upstream, in `Nameless.Windows` 4.4.0 (its own ADR-002 and ADR-003). This ticket adopts the pattern from the mandated reference sample (`Nameless.WinApp`) and does not change it.
- Out of scope, as the ticket says: every `*.xaml.cs` file, plus `Displays.resx` and `Validations.resx`.

## 1. Context: what exists today (checked in this branch and in package 4.4.0)

| Concern | Reality | Consequence |
|---|---|---|
| Package | `Directory.Packages.props` pins `Nameless.Windows` 4.4.0. Its XML docs list `JsonTranslator`, `ITranslator<T>`, `Translator<T>`, `TranslatorFactory`, `TranslationKey` and `Json.FileSystemTranslationFileProvider`. There is no `ResourceTranslator`. The local library source at `C:\Workspace\Nameless\libs\windows\src\Nameless.Windows\Localization` matches. | The resx files no longer feed the runtime translator. |
| Host wiring | `Infrastructure/ApplicationHostFeature.cs` already calls `WithUseAssemblyScan(false).WithTranslator<JsonTranslator>()`. Its comment still mentions `ResourceTranslator`, which is stale. | No change to DI code is needed. Only the comment should be fixed. |
| Config | `appsettings.json` has `"Translator": { "ResourceFullName": "..." }`. | **The app is broken at startup today.** The `Translator` section exists, so `TranslatorHostFeature` registers `JsonTranslator` and `FileSystemTranslationFileProvider`. `RootPath` is null, so the provider's constructor throws `MissingConfigurationException` the first time anything resolves `ITranslator`. That happens in `TranslatorExtension.ResolveTranslator` (App.OnCurrentHostStart) and in the bootstrap step. The tests miss this because they all use fakes. This ticket fixes it (D1). |
| Section rules | `TranslationKey.GetSection(typeof(T))` is `T.Name` with the generic arity removed. `SectionTranslator` turns `this[key]` into the root lookup `{Section}_{key}`. A null or whitespace key goes to the root unchanged, so `[""]` returns `string.Empty`. `JsonTranslator` flattens `{ "Section": { "Key": "Text" } }` into `Section_Key`. | **A section named after a class prefix gives a flat key equal to the old resx key.** For example, section `PerformApplicationBackupRequestHandler` with key `Preparing` composes to `PerformApplicationBackupRequestHandler_Preparing`. This is what D3 relies on. |
| Parser strictness | `JsonTranslator.TryParseFile` throws away the **whole file** (it logs and returns null, so every lookup shows `[key]`) on any of these: a duplicate property, an empty or whitespace value, an empty key, or two sections composing to the same flat key. It always runs `string.Format(template, parameters)`, even when there are no parameters. | One typo hides every translation in that culture. A literal `{`/`}` that is not an indexed placeholder throws `FormatException` at lookup time. The fitness tests in section 4 guard against both. |
| Fallback | Lookup order is `pt-BR` → `pt` → `default`, merged one key at a time. | `default.json` holds the English text and `pt-BR.json` holds the Portuguese text. There is no `en.json`. |
| Root-translator readers that stay on flat keys | (a) Every `{loc:Translator Some_Key}` in `*.xaml`: AppConfigurationPage, DocumentDirectoryManagementPage, DocumentDirectoryWindow, SearchPage and two user controls. (b) `MainWindow.xaml.cs` (`MainWindow_ConfirmApplicationExit_*`, out of scope). (c) The library's `TaskRunnerWindowViewModel` and the TaskRunner XAML (`TaskRunnerWindow_*`). | After this ticket these read **JSON** through the root `JsonTranslator`. If the JSON lacks their keys, the screens show `[AppConfigurationPage_Title]` and similar placeholders (D3). `{loc:Translator}` has no section property, so a XAML key is always the full flat key (for the `UI` section: `UI_<key>`). Keys in (b) are fixed by an out-of-scope file and keys in (c) by the library, so this ticket cannot rename them (D3, D3b). |
| resx consumers that stay | `Views/Pages/AppConfigurationPage.xaml.cs`, `DocumentDirectoryManagementPage.xaml.cs` and `SearchPage.xaml.cs` all declare `[NavigationViewItem(..., ResourceType = typeof(Localization))]`. `Localization` is the generated `Localization.Designer.cs` class. `LocalizationFitnessTests` reads `Localization.ResourceManager`. | **The resx files cannot be deleted in this ticket** without editing `.xaml.cs` files, which is forbidden (D5). |

## 2. Decisions

### D1: File location and wiring, copied from the sample

- New files: `src/Nameless.InfoPhoenix.Client/Resources/loc/default.json` and `Resources/loc/pt-BR.json`. Use the sample's `loc` folder name. Do not reuse `l10n`, so the old and new mechanisms stay separate until the resx files are deleted. The full content is in appendices A and B.
- Encoding: UTF-8 without a BOM, LF or CRLF line endings. `File.ReadAllText` handles either.
- `Nameless.InfoPhoenix.Client.csproj`: in the existing `<ItemGroup>` that holds `appsettings.json`'s `Content`, add:
  ```xml
  <!-- JSON translation files read by JsonTranslator's disk-backed
       ITranslationFileProvider (see TranslatorOptions.RootPath). -->
  <Content Include="Resources\loc\default.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Resources\loc\pt-BR.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  ```
  These are not embedded resources. `FileSystemTranslationFileProvider` reads them from disk, relative to `AppContext.BaseDirectory`, and watches them for hot-reload.
- `appsettings.json`: replace the dead `"Translator": { "ResourceFullName": ... }` block with the sample's shape:
  ```json
  "Translator": {
    "RootPath": "Resources\\loc"
  }
  ```
  Remove `ResourceFullName` completely, because no options type binds it anymore. Do not set `DefaultCulture`. The library documents it as unused, and the culture comes from `IAppConfigurationManager.Language` through `TranslatorExtension.ResolveTranslator`.
- Leave the DI code alone. `ApplicationHostFeature.ConfigureTranslatorHostFeature` already registers `JsonTranslator`. `TranslatorHostFeature` always registers `ITranslatorFactory` and the open-generic `ITranslator<>` → `Translator<>` as singletons. Only fix the stale comment, which still says `WithTranslator<ResourceTranslator>()`, so it reads `JsonTranslator`.

### D2: Moving each class to `ITranslator<T>`: drop the prefix, keep the suffix

For each of the 9 classes:
- Change the constructor parameter and field or property type from `ITranslator` to `ITranslator<TSelf>`.
- Remove the class-name prefix from every indexer key. The section-relative key is **the old flat key with its `{Prefix}_` part removed**. The suffixes stay exactly as they are, with no renames to match the sample's method-based naming. This is the smallest change and keeps test churn low.
- Remove any `Prefix` constant that becomes unused. With `TreatWarningsAsErrors`, leaving it could break the build through an analyzer.
- Keep `using Nameless.Windows.Localization;` because `ITranslator<T>` lives in the same namespace.

| Class (= JSON section) | Old lookup | New lookup |
|---|---|---|
| `EntityFrameworkCoreExecuteMigrationStep` | `_translator["EntityFrameworkCoreExecuteMigrationStep_DisplayName"]`, `..._ExecuteAsync_Starting`, `..._ExecuteAsync_Finished` | `_translator["DisplayName"]`, `["ExecuteAsync_Starting"]`, `["ExecuteAsync_Finished"]` |
| `DocumentDirectoryWindowViewModelValidator` | `translator["Label_Empty_Error_Message"]` and 4 more (no prefix) | **Unchanged keys.** Only the parameter type changes to `ITranslator<DocumentDirectoryWindowViewModelValidator>`. |
| `PerformApplicationRestoreRequestHandler` | `_translator[$"{Prefix}_{stepName}"]`, `[$"{Prefix}_{key}"]`, `[$"{Prefix}_{key}", args]`, `[$"{Prefix}_Success"]` | `_translator[stepName]`, `[key]`, `[key, args]`, `["Success"]`. Remove `Prefix`. |
| `PerformApplicationBackupRequestHandler` | `_translator[$"{Prefix}_{step}"]`, `[$"{Prefix}_{step}_Failure", [..]]`, `[$"{Prefix}_{warningKey}_Warning"]`, `[$"{Prefix}_Success", [..]]` | `_translator[step]`, `[$"{step}_Failure", [..]]`, `[$"{warningKey}_Warning"]`, `["Success", [..]]`. Remove `Prefix`. |
| `OpenCurrentLogFileRequestHandler` | `_translator["OpenCurrentLogFileRequestHandler_Missing_Log_File_Title"]` / `_Message` | `_translator["Missing_Log_File_Title"]` / `["Missing_Log_File_Message"]` |
| `GetDocumentDirectoryRequestHandler` (in `GetDocumentDirectoryRequest.cs`) | `_translator[$"{nameof(GetDocumentDirectoryRequestHandler)}_{nameof(HandleAsync)}_MissingDocumentDirectory", request.ID]` | `_translator[$"{nameof(HandleAsync)}_MissingDocumentDirectory", request.ID]` |
| `DocumentDirectoryWindowViewModel` | `_translator["FileSystemDialog_Title"]` (no prefix) | **Unchanged key.** Type change only. |
| `DocumentDirectoryManagementPageViewModel` | `_translator[""]` (the window title in `CreateNewDocumentDirectoryAsync`) | **Unchanged.** `[""]` still returns `string.Empty` through `SectionTranslator` → root, which is the same as today. Inventing a key and copy for an unfinished feature is out of scope (see section 8, O1). |
| `AppConfigurationPageViewModel` | `T["AppConfigurationPage_InvalidSettings_Message"]` and 12 more | `T["InvalidSettings_Message"]` and so on. Remove the `AppConfigurationPage_` prefix from all 13 keys. The section is **`AppConfigurationPageViewModel`**, not `AppConfigurationPage`. |

`AppConfigurationPageViewModel.T.SetCulture(...)`, used for the language switch, keeps working. `Translator<T>` → `SectionTranslator` → root `JsonTranslator.SetCulture`, which raises `Item[]` on the root, so every XAML binding refreshes. Lifetimes are safe: `ITranslator<T>` is a singleton, and injecting a singleton into transient or scoped handlers and view models is allowed.

### D3 (user decision): one shared `UI` section for view text, keys named by component

The root `JsonTranslator` is now the only translator, so the XAML markup keys read JSON. The JSON must contain them, or this ticket turns every label on those screens into `[Key]`. **User decision:** view text goes in **one** top-level section named `UI` in both files, not one section per view. Keys inside `UI` name the **component** that shows the text, not the view.

**Key naming rule** (apply it the same way to every future `UI` key):
1. Key = `<Component>_<Text>`. `<Component>` is the WPF control type that shows the text, without the XML namespace prefix: `TextBlock`, `Button`, `Label`, `Badge`, `ToolTip` (the `ToolTip` attribute counts as its own component). `<Text>` is the English text in PascalCase with punctuation dropped, for example `Button_CheckForUpdate`. For a full sentence, use a short PascalCase summary of 2 to 4 words that starts with its verb or subject, for example `TextBlock_ChooseTheme` for "Choose the color theme used across the application."
2. **Collision, same text:** when two views use the same `<Component>_<Text>` and the text is identical in **every** culture file (default and pt-BR), keep one shared key.
3. **Collision, different text:** when the name is the same but the text differs in at least one culture, give each one a qualifier word in front that names its context, for example `CardUpdateApp_Button_Cancel`. Do not qualify a key that has no collision.
4. A key is identified by component plus text, so the same English text on a different component (for example `TextBlock_OpenLogFile` and `Button_OpenLogFile`) is not a collision.

**Collision check result** for the 30 keys taken from the resx source (appendix A): **there are no different-text collisions.** The only same-name candidate is `Button_Close`, used by `DocumentDirectoryWindow` ("Close" / "Fechar") and the library's TaskRunner window ("Close" / "Fechar"). The text is identical in both cultures, so it would be one shared key. The TaskRunner window cannot read `UI` (see D3b), so in practice only `DocumentDirectoryWindow` uses it. A script checked the list for duplicate names and identical text.

**Consequence: XAML changes.** `{loc:Translator}` has no section parameter, and `UI` + `Button_Save` composes to `UI_Button_Save`. The old flat key (`DocumentDirectoryWindow_SaveButton_Caption`) cannot be produced from the `UI` section. The developer must therefore change the key in each `{loc:Translator ...}` of the three `.xaml` files below. The ticket excludes `*.xaml.cs` only, so `.xaml` files are in scope. Only the markup-extension key changes, and no other attribute does.

| Old flat key (in `.xaml`) | New XAML key |
|---|---|
| `AppConfigurationPage_Title` | `UI_TextBlock_Settings` |
| `AppConfigurationPage_VersionCard_Title` | `UI_TextBlock_Version` |
| `AppConfigurationPage_ComingSoon_Badge` | `UI_Badge_ComingSoon` |
| `AppConfigurationPage_VersionCard_Button_Caption` | `UI_Button_CheckForUpdate` |
| `AppConfigurationPage_ComingSoon_Tooltip` | `UI_ToolTip_NotAvailableYet` |
| `AppConfigurationPage_ApplicationLanguageCard_Title` | `UI_TextBlock_Language` |
| `AppConfigurationPage_ApplicationLanguageCard_Message` | `UI_TextBlock_ChooseLanguage` |
| `AppConfigurationPage_ApplicationBackupCard_Title` | `UI_TextBlock_Backup` |
| `AppConfigurationPage_ApplicationBackupCard_Message` | `UI_TextBlock_CreateBackup` |
| `AppConfigurationPage_ApplicationBackupCard_Button_Caption` | `UI_Button_CreateBackup` |
| `AppConfigurationPage_ApplicationRestoreCard_Title` | `UI_TextBlock_Restore` |
| `AppConfigurationPage_ApplicationRestoreCard_Message` | `UI_TextBlock_RestoreFromBackup` |
| `AppConfigurationPage_ApplicationRestoreCard_Button_Caption` | `UI_Button_RestoreBackup` |
| `AppConfigurationPage_ThemeCard_Title` | `UI_TextBlock_Theme` |
| `AppConfigurationPage_ThemeCard_Message` | `UI_TextBlock_ChooseTheme` |
| `AppConfigurationPage_ExitBehaviorCard_Title` | `UI_TextBlock_ConfirmBeforeExit` |
| `AppConfigurationPage_ExitBehaviorCard_Message` | `UI_TextBlock_AskBeforeExit` |
| `AppConfigurationPage_AppDirectoryPathCard_Title` | `UI_TextBlock_ApplicationDataDirectory` |
| `AppConfigurationPage_AppDirectoryPathCard_Message` | `UI_TextBlock_OpenDataDirectory` |
| `AppConfigurationPage_AppDirectoryPathCard_Button_Caption` | `UI_Button_OpenDirectory` |
| `AppConfigurationPage_ApplicationLogCard_Title` | `UI_TextBlock_ApplicationLog` |
| `AppConfigurationPage_ApplicationLogCard_Message` | `UI_TextBlock_OpenLogFile` |
| `AppConfigurationPage_ApplicationLogCard_Button_Caption` | `UI_Button_OpenLogFile` |
| `DocumentDirectoryManagementPage_Title` | `UI_TextBlock_DocumentsDirectoriesManagement` |
| `DocumentDirectoryManagementPage_CreateNewDocumentDirectoryButton_Caption` | `UI_Button_CreateNewDocumentDirectory` |
| `DocumentDirectoryWindow_DocumentDirectory_Label` | `UI_Label_Label` |
| `DocumentDirectoryWindow_DocumentDirectory_DirectoryPath` | `UI_Label_DirectoryPath` |
| `DocumentDirectoryWindow_DocumentDirectory_DisplayPosition` | `UI_Label_Position` |
| `DocumentDirectoryWindow_SaveButton_Caption` | `UI_Button_Save` |
| `DocumentDirectoryWindow_CloseButton_Caption` | `UI_Button_Close` |

The text in each culture is exactly the resx text, unchanged. Only the key changes.

Not added, because there is no source text in the resx today and those screens already show `[Key]` before this ticket: `SearchPage.xaml` (`DocumentSearchPage_*`), `DisplayDocumentDirectoryUserControl_*` and `DocumentDirectorySearchEntryUserControl_*`. Leave their XAML keys as they are. When their text is written, it goes into `UI` under the same rule. The navigation titles (`*_NavigationViewItem_Title`) are also not added, because they still come from the resx through `ResourceType = typeof(Localization)` in `.xaml.cs` (D5). Do not add unused resx keys such as `DocumentDirectoryWindow_Label`, `_DirectoryPath` or `_DisplayPosition`.

### D3b (NEEDS-DECISION): `MainWindow` and `TaskRunnerWindow` keys cannot move into `UI` in this ticket

Two of the five views from the original D3 do not read their text through `.xaml` that this ticket can edit:
- `MainWindow`: the keys `MainWindow_ConfirmApplicationExit_MessageBox_Title` and `_Message` are string literals in `MainWindow.xaml.cs`, which the ticket excludes. No `UI_*` key can compose to `MainWindow_*`.
- `TaskRunnerWindow`: the keys `TaskRunnerWindow_CancelButton_Caption`, `_CloseButton_Caption`, `_Cancelled_Message` and `_CloseBlocked_Message` are fixed inside the `Nameless.Windows` package, in `TaskRunnerWindow.xaml` and `TaskRunnerWindowViewModel.cs`. They are documented there as the library's contract. This repository cannot change them in any ticket.

| Option | What it costs |
|---|---|
| **A (recommended, and what the appendices contain):** keep `MainWindow` and `TaskRunnerWindow` as two small sections whose keys match the hard-coded ones, next to `UI`. | Two sections besides `UI`, so the user's "single section" applies to the three views whose markup this ticket edits. There is no regression. `TaskRunnerWindow` stays permanently, because the library owns its keys. `MainWindow` moves into `UI` in the follow-up that edits `MainWindow.xaml.cs`, as `UI_MessageBox_ExitApplication_Title` / `_Message` (no collision). |
| B: move the `MainWindow` keys into `UI` now and widen the scope to the two string literals in `MainWindow.xaml.cs`. | A scope change to an excluded file (two lines and `MainWindowTests`). The `TaskRunnerWindow` section is still required. |
| C: strict `UI` only. Drop both sections. | The exit confirmation shows `[MainWindow_ConfirmApplicationExit_MessageBox_Title]` until the follow-up. The TaskRunner Cancel/Close buttons and its cancelled and close-blocked messages show raw keys **permanently**, unless the library changes. This is not recommended. |

If the user picks C, delete the `MainWindow` and `TaskRunnerWindow` sections from both appendices and their pairs from F1. If the user picks B, move the two `MainWindow` entries into `UI` under the names above and edit the two literals.

### D4: New copy for the 7 keys that have no resx source

Three in-scope classes look up keys that **do not exist** in `Localization.resx` or `Localization.pt-BR.resx`. Today they render as `[key]` (before the package change) or crash (after it). The JSON parser rejects empty values, so each key needs real text. The proposed text below is in appendices A and B. Settle the final wording in PR review.

| Section | Key | default (en) | pt-BR |
|---|---|---|---|
| `DocumentDirectoryWindowViewModelValidator` | `Label_Empty_Error_Message` | The label is required. | O rótulo é obrigatório. |
| | `DirectoryPath_Empty_Error_Message` | The directory path is required. | O caminho do diretório é obrigatório. |
| | `DirectoryPath_MaximumLength_Error_Message` | The directory path is too long. | O caminho do diretório é muito longo. |
| | `DirectoryPath_Unique_Error_Message` | This directory is already registered. | Este diretório já está cadastrado. |
| | `DisplayPosition_LowerOrEqualTo_Error_Message` | The display position is not valid. | A posição de exibição não é válida. |
| `DocumentDirectoryWindowViewModel` | `FileSystemDialog_Title` | Select a document directory | Selecione um diretório de documentos |
| `GetDocumentDirectoryRequestHandler` | `HandleAsync_MissingDocumentDirectory` | Document directory not found ({0}). | Diretório de documentos não encontrado ({0}). |

Rule for the validator text: **no braces at all**. FluentValidation's `{PropertyName}` placeholders would reach `JsonTranslator`'s `string.Format` first and throw `FormatException`.

`DocumentDirectoryManagementPageViewModel` gets an **empty section** (`"DocumentDirectoryManagementPageViewModel": {}`). The parser accepts it. It satisfies the ticket's "one section per class" item literally and marks where the future window-title key goes. Leaving it out would work just as well at runtime.

### D5: Keep `Localization.resx`, `Localization.pt-BR.resx` and `Localization.Designer.cs`. Do not prune them. The follow-up ticket deletes them.

- They **cannot** be deleted now. Three `.xaml.cs` files (`AppConfigurationPage`, `DocumentDirectoryManagementPage`, `SearchPage`) reference `typeof(Localization)` in `[NavigationViewItem(ResourceType = ...)]`. Deleting the files breaks the build, and fixing that means editing `.xaml.cs`, which this ticket forbids. The navigation titles also still come from the resx at runtime.
- Do not prune the migrated entries either. Pruning touches the files for no runtime benefit, because nothing reads them except the navigation attribute. It also makes the follow-up harder to review. Leave the csproj `EmbeddedResource`/`Compile Update` entries for them untouched.
- **Follow-up ticket (handed over):** migrate the `.xaml.cs` classes, which means `MainWindow` switches its two literals to `UI_MessageBox_ExitApplication_Title` / `_Message` (it keeps the root `ITranslator`, because view text lives in the shared `UI` section and there is no `UI` type to close `ITranslator<T>` over) and the three `[NavigationViewItem]` attributes → `TitleResourceName` with no `ResourceType`, as in the sample's `AppConfigurationPage.xaml.cs`. Add the navigation titles to the `UI` section under the D3 naming rule (for example `UI_NavigationViewItem_Settings`), pointing `TitleResourceName` at the composed `UI_*` key, and move the `MainWindow` keys into `UI` (D3b). Then delete the three `Localization*` files and their csproj entries.
- Interim caveat for the reviewer and the CHANGELOG: editing migrated text in `Localization*.resx` now has **no effect**. JSON is the source of truth for everything except the three navigation titles.

## 3. Test changes (existing tests must keep compiling and passing)

1. **A typed fake that reuses the real composition.** Add a test helper, for example in `Fakes/FakeTranslator.cs`:
   ```csharp
   internal static ITranslator<T> For<T>(this ITranslator root) => new Translator<T>(new TranslatorFactory(root));
   ```
   `Translator<T>` and `TranslatorFactory` are public. Wrapping the existing `FakeTranslator` (or `FormattingTranslator`, `PassthroughTranslator`, `RecordingCultureAtPublishTimeTranslator`) this way runs the **real** `{Section}_{key}` composition, and the root fake records and echoes the composed key. As a result:
   - Backup, restore and open-log handler tests: the echoed or asserted strings are **unchanged**, because section = old prefix. Only the construction changes, to `new FakeTranslator().For<PerformApplicationBackupRequestHandler>()`.
   - `AppConfigurationPageViewModel*Tests` and `AppConfigurationPageTests`: assertions on `"AppConfigurationPage_Restore_..."` and similar become `"AppConfigurationPageViewModel_Restore_..."`. Keep a reference to the root fake for `Requests` and `SetCultureCalls`. `SetCulture` forwards to the root.
   - The XAML badge assertion in `AppConfigurationPageTests.StubBadge_ContentResolvesThroughTranslator` (through `TranslatorExtension.SetTranslator(new FakeTranslator())`) changes from `"AppConfigurationPage_ComingSoon_Badge"` to `"UI_Badge_ComingSoon"`, because the XAML key changes (D3). XAML still uses the root translator. Update any other test that asserts on an old XAML flat key in the same way, using the D3 mapping table.
   - `MainWindowTests` keeps passing a non-generic `FakeTranslator`, because `MainWindow` is out of scope.
   - Do not hand-roll a `FakeTranslator<T>` that skips composition. It would hide the exact bug this ticket can introduce: a key that keeps its old prefix and becomes `Section_Section_Key`.
2. **Retarget `Views/Pages/LocalizationFitnessTests.cs` from resx to JSON** (fitness function F1 below). Today it checks `Localization.ResourceManager`. After this ticket that check passes but proves nothing about runtime. Change the `Keys` data to `(section, key)` pairs covering every entry in appendix A. Section ownership changes for the 13 `AppConfigurationPage_*` view-model keys, which now live in `AppConfigurationPageViewModel`. The 30 view keys become `("UI", "<Component>_<Text>")` pairs, such as `("UI", "Button_Save")`, and `MainWindow_*` / `TaskRunnerWindow_*` become `("MainWindow", ...)` / `("TaskRunnerWindow", ...)` (D3b, option A). Keep the existing rule: non-empty and pt-BR differs from default. Every pair in the appendices meets it, checked by script.

## 4. Quality attributes and fitness functions

| Attribute | How the design serves it | Fitness function (developer implements) |
|---|---|---|
| **Correctness of localization** (no `[Key]` at runtime) | Exact key inventory (appendices). Section = class name. Prefix-stripping rule (D2). | **F1** (retargeted `LocalizationFitnessTests`): read `Resources/loc/default.json` and `pt-BR.json` from `AppContext.BaseDirectory`. Check that they copy to test output through the project reference's `Content`. If they do not, link them in the test csproj. Deserialize with `JsonSerializerOptions { AllowDuplicateProperties = false }`, as the library does, into `Dictionary<string, Dictionary<string,string>>`. For each required `(section, key)`, assert that both files contain it, that both values are non-empty and that the values differ. **F2**: for every entry, the set of `{n}` placeholders in default equals the set in pt-BR, and there is no brace that is not `{digits}`. This prevents `FormatException`. **F3**: the composed keys `section_key` are unique within each file, which mirrors the library's collision rejection. **F3b** (keeps the D3 rule true): every key in the `UI` section matches `^([A-Za-z]+_)?(TextBlock|Button|Label|Badge|ToolTip|MessageBox)_[A-Za-z]+$`, and both files have the same set of `UI` keys. Extend the component list when a new control type appears. **F3c** (XAML ↔ JSON): scan every `*.xaml` under `Views/` for `{loc:Translator X}`. Every `X` that starts with `UI_` must exist as a composed key in both files. This catches a mistyped XAML key that would otherwise show `[UI_...]`. Keys of screens that have no text yet (D3 "Not added") are listed explicitly as allowed gaps. |
| **Operability / startup** | D1 fixes the startup crash: `Translator` section with no `RootPath`. | **F4**: build an `IConfiguration` from the output `appsettings.json`, then assert that `Translator:RootPath` is non-empty and that `Path.Combine(AppContext.BaseDirectory, rootPath, "default.json")` and `"pt-BR.json"` exist. **F5** (smoke): construct the real `JsonTranslator(new FileSystemTranslationFileProvider(Options.Create(new TranslatorOptions { RootPath = <that path> }), NullLogger<...>.Instance), NullLogger<JsonTranslator>.Instance)`. Assert that `translator["UI_TextBlock_Settings"]`, `translator["TaskRunnerWindow_CancelButton_Caption"]` and `new TranslatorFactory(translator).Create<PerformApplicationBackupRequestHandler>()["Preparing"]` do not start with `[`. This proves the real parser accepts both files. Do not call `SetCulture` in tests: it changes `CultureInfo.CurrentCulture`/`CurrentUICulture`, so either value is acceptable. Dispose both. |
| **Evolvability** (the follow-up can delete resx cleanly) | Every in-scope non-view class uses `ITranslator<T>`. View text lives in one shared `UI` section with a mechanical naming rule (D3), so new screens add keys without new sections. | **F6** (architecture test, reflection, can sit next to `ConstantsFitnessTests`): no public constructor of a type in the client assembly takes a parameter of the exact type `Nameless.Windows.Localization.ITranslator`, **except** types deriving from `System.Windows.Window`, `System.Windows.Controls.Page` or `System.Windows.Controls.UserControl`. Those read the shared `UI` section through the root translator, so the exception is permanent and not a pending migration. |

## 5. Test strategy per scope item

| Scope item | Test level | What to cover |
|---|---|---|
| 1. JSON files + wiring | Unit / fitness (F1–F5, F3b, F3c) | The files exist in output, parse under the library's strict rules, contain every key in both cultures, have matching placeholders and resolve through the real `JsonTranslator`. The `UI` keys follow the naming rule, and every `UI_*` XAML key exists in JSON. |
| 2. `ITranslator<T>` in 9 classes | Unit (existing suites, rewired per section 3) + F6 | The existing handler and view-model tests stay green with the real composition. F6 blocks regressions back to non-generic. |
| 3. Key migration | Unit, through the `For<T>()` composition | Existing assertions that echo keys catch an unstripped prefix, because it would produce `PerformApplicationBackupRequestHandler_PerformApplicationBackupRequestHandler_Preparing`. |
| Manual (QA) | Run the app | It starts with no exception. Settings page text appears in English and in pt-BR after a language switch. Backup and restore progress messages are translated. Exit confirmation is translated (D3). TaskRunner Cancel/Close are translated. |

## 6. Work split and developer tasks (in build order)

**Backend (`backend-developer-dotnet`)**: one branch, `feature/IPX-0015-MigrateTranslatorToJsonTranslator`.

1. Add `Resources/loc/default.json` and `Resources/loc/pt-BR.json` with exactly the content in appendices A and B: the `UI` section, the `MainWindow` and `TaskRunnerWindow` sections (D3b option A, unless the user decides otherwise) and the class sections (scope items 1 and 3).
1b. In `AppConfigurationPage.xaml`, `DocumentDirectoryManagementPage.xaml` and `DocumentDirectoryWindow.xaml`, replace each `{loc:Translator <old key>}` with its `UI_*` key from the D3 mapping table. Do not touch any `.xaml.cs` file, and leave `SearchPage.xaml` and the two user controls as they are (D3).
2. csproj `Content Include` entries and `appsettings.json` `Translator:RootPath` (remove `ResourceFullName`). Fix the stale comment in `ApplicationHostFeature` (scope item 1, D1).
3. Move the 9 classes to `ITranslator<T>` and strip the prefixes per the D2 table. Remove the unused `Prefix` constants (scope item 2).
4. Test helper `For<T>()` and rewire the existing tests (section 3.1).
5. Retarget `LocalizationFitnessTests` to JSON (F1–F3) and add F3b, F3c, F4, F5 and F6.
6. `dotnet build` (warnings are errors) and `dotnet test`. Then run the app once in each language and check the manual items in section 5.

Commits per the project's convention, for example `refactor(IPX-0015): move translators to ITranslator<T> and JSON translation files`. Mark nothing as breaking: this is a desktop app with no external consumers.

## 7. Security notes (for the developer and QA)

- Translation files are read from the install directory, and `FileSystemTranslationFileProvider` watches them for hot-reload. Anyone who can write to the install folder can change UI text. That is the same trust boundary as the binaries, so this is acceptable and not a new attack surface.
- Text goes into `string.Format` with arguments that sometimes come from untrusted input: backup file names, archive entry names in `Rejected_UnexpectedEntry`, and `ex.Message`. These are **arguments**, not templates, so they cannot inject format specifiers. Keep it that way: never pass user-controlled text as the *key* or the template.
- No secrets or PII are added. `JsonTranslator` logs missing keys at Debug level with the key name only.

## 8. Risks for QA to probe, and observations

- **One bad entry blanks a whole culture.** Watch for a duplicate key, an empty value or a composed-key collision. F1–F3 guard this, but QA should also hand-edit `pt-BR.json` in the output folder while the app runs. Hot-reload with invalid JSON should keep the last good text, and a fix should reapply it.
- **Unstripped prefix** (`Section_Section_Key`) shows up as `[...]`. Check every backup and restore step, warning and rejection path. The restore rejection keys are built dynamically from `Rejected_{ReasonKey}`, covering 14 reasons in total.
- **Language switch:** after switching on the Settings page, the XAML labels, the view-model messages and the TaskRunner buttons all change together. They all share the one root instance.
- **The validator captures messages once, at construction.** `DocumentDirectoryWindowViewModelValidator` calls `translator[...]` inside its constructor, so its messages stay in the culture that was active when it was built. This behavior predates this ticket and is not changed here. It is worth a follow-up issue together with the observations below.
- O1: `DocumentDirectoryManagementPageViewModel.CreateNewDocumentDirectoryAsync` sets the window title to `_translator[""]`, which is always empty. This is unfinished feature work and is left as is.
- O2, outside scope and only noted: the validator's `DisplayPosition` rule is `LessThanOrEqualTo(-1)`, and `UniqueDirectoryPathAsync` returns `AnyAsync(...)`, which is true when the path *already exists*. Both look inverted.
- O3: the library's `Bootstrapper` looks up `Bootstrapper_*` keys, and the splash screen may show them. They were never in the resx and are out of scope. A `Bootstrapper` section like the sample's could be added later.

## 9. Open decisions

- **D3: decided by the user.** There is one `UI` section with component-named keys and a collision rule. Applying it requires the XAML key edits in task 1b.
- **D3b: NEEDS-DECISION.** The `MainWindow` and `TaskRunnerWindow` keys are hard-coded in an excluded file and in the library, so they cannot compose from `UI`. The recommendation is option A, which keeps two small sections next to `UI` and is what the appendices contain. B widens scope to `MainWindow.xaml.cs`. C regresses the exit dialog and, permanently, the TaskRunner window. See D3b for the costs. Implementation can start on option A, because B and C are pure deletions or moves from it.

## Appendix A: `Resources/loc/default.json` (binding content)

The English text is copied verbatim from `Localization.resx`. Line breaks inside multi-line messages are written as `
`. The 7 new keys from D4 and the empty `DocumentDirectoryManagementPageViewModel` section are included. View text sits in the single `UI` section (D3, 30 keys). `MainWindow` and `TaskRunnerWindow` stay as separate sections under D3b option A. The file has 12 sections and 106 keys. A script checked it for valid JSON, no composed-key collisions, no empty values, placeholder parity with pt-BR and no stray braces.

```json
{
  "AppConfigurationPageViewModel": {
    "InvalidSettings_Title": "Settings reset",
    "InvalidSettings_Message": "Your saved settings could not be read and were reset to the defaults.",
    "Backup_TaskRunnerWindow_Title": "Back up application data",
    "Backup_Success_Message": "Backup created: {0}",
    "Backup_Failure_Message": "Backup failed unexpectedly: {0}",
    "Restore_SelectFile_Title": "Select a backup to restore",
    "Restore_SelectFile_FilterName": "InfoPhoenix backups",
    "Restore_ConfirmOverwrite_Title": "Restore backup",
    "Restore_ConfirmOverwrite_Message": "Restoring \"{0}\" will replace all current application data (database, search index and settings) with the contents of this backup.\n\nYour current data will be overwritten and cannot be recovered unless you have backed it up.\n\nDo you want to continue?",
    "Restore_Failure_Message": "The restore failed unexpectedly: {0}",
    "Restore_TaskRunnerWindow_Title": "Restore application data",
    "Restore_Relaunch_Title": "Restore complete",
    "Restore_Relaunch_Message": "Your backup was restored.\n\nClose and reopen InfoPhoenix to start using the restored data. Until you do, some settings and information on screen may be out of date."
  },
  "DocumentDirectoryManagementPageViewModel": {},
  "DocumentDirectoryWindowViewModel": {
    "FileSystemDialog_Title": "Select a document directory"
  },
  "DocumentDirectoryWindowViewModelValidator": {
    "Label_Empty_Error_Message": "The label is required.",
    "DirectoryPath_Empty_Error_Message": "The directory path is required.",
    "DirectoryPath_MaximumLength_Error_Message": "The directory path is too long.",
    "DirectoryPath_Unique_Error_Message": "This directory is already registered.",
    "DisplayPosition_LowerOrEqualTo_Error_Message": "The display position is not valid."
  },
  "EntityFrameworkCoreExecuteMigrationStep": {
    "DisplayName": "Application Database Migration Step",
    "ExecuteAsync_Starting": "Initializing application database migration...",
    "ExecuteAsync_Finished": "Application Database Migration finished successfully!"
  },
  "GetDocumentDirectoryRequestHandler": {
    "HandleAsync_MissingDocumentDirectory": "Document directory not found ({0})."
  },
  "MainWindow": {
    "ConfirmApplicationExit_MessageBox_Title": "Exit application",
    "ConfirmApplicationExit_MessageBox_Message": "Are you sure you want to exit? Choose \"No\" to stop asking."
  },
  "OpenCurrentLogFileRequestHandler": {
    "Missing_Log_File_Title": "Log file not found",
    "Missing_Log_File_Message": "No application log file was found yet."
  },
  "PerformApplicationBackupRequestHandler": {
    "Preparing": "Preparing backup...",
    "CopyingConfiguration": "Saving and copying settings...",
    "CopyingDatabase": "Copying database...",
    "CopyingSearchIndex": "Copying search index...",
    "CreatingArchive": "Creating backup file...",
    "Success": "Backup created: {0}",
    "ConfigurationMissing_Warning": "No settings file found; backup continues without it.",
    "DatabaseMissing_Warning": "No database found; backup continues without it.",
    "SearchIndexMissing_Warning": "No search index found; backup continues without it.",
    "Preparing_Failure": "Backup failed while preparing: {0}",
    "CopyingConfiguration_Failure": "Backup failed while copying settings: {0}",
    "CopyingDatabase_Failure": "Backup failed while copying the database: {0}",
    "CopyingSearchIndex_Failure": "Backup failed while copying the search index: {0}",
    "CreatingArchive_Failure": "Backup failed while creating the backup file: {0}"
  },
  "PerformApplicationRestoreRequestHandler": {
    "ReadingBackup": "Checking the backup file...",
    "ExtractingBackup": "Extracting the backup to a temporary folder...",
    "VerifyingBackup": "Verifying the extracted data...",
    "PreparingSwap": "Preparing to replace the current data...",
    "Swapping": "Replacing the current data...",
    "Success": "Restore complete. Restart InfoPhoenix to use the restored data.",
    "VersionDiffers_Warning": "This backup was created by InfoPhoenix {0}; you are running {1}. The data format is compatible, so the restore continues.",
    "DatabaseAbsent_Warning": "This backup has no database; the current database will be removed.",
    "SearchIndexAbsent_Warning": "This backup has no search index; the current search index will be removed.",
    "ConfigurationAbsent_Warning": "This backup has no settings; your settings will return to their defaults.",
    "CleanupIncomplete_Warning": "The previous data at {0} could not be deleted. You can delete it manually.",
    "Rejected_SelectedFileInDataFolder": "The selected file is inside InfoPhoenix's own data folders. Move it elsewhere (for example, to the backups folder) and try again. Nothing was changed.",
    "Rejected_NotAnArchive": "The selected file is not a valid backup file. Nothing was changed.",
    "Rejected_ManifestMissing": "The selected file is not an InfoPhoenix backup. Nothing was changed.",
    "Rejected_ManifestInvalid": "The backup information in the selected file is unreadable or incomplete. Nothing was changed.",
    "Rejected_WrongApplication": "The selected file is a backup of another application ({0}). Nothing was changed.",
    "Rejected_UnsupportedFormat": "This backup uses format version {0}, which this version of InfoPhoenix cannot read. Nothing was changed.",
    "Rejected_SchemaMismatch": "This backup's database version ({0}) does not match this version of InfoPhoenix ({1}). Restoring it is not supported. Nothing was changed.",
    "Rejected_UnexpectedEntry": "The backup contains an unexpected or unsafe item: {0}. Nothing was changed.",
    "Rejected_TooManyEntries": "The backup contains too many items ({0}, maximum {1}). Nothing was changed.",
    "Rejected_ContentsMismatch": "The backup's contents do not match its description; the file may be damaged. Nothing was changed.",
    "Rejected_InsufficientSpace": "There is not enough free disk space to restore this backup ({0} needed). Nothing was changed.",
    "Rejected_Corrupted": "The backup file is damaged: {0}. Nothing was changed.",
    "Rejected_ConfigurationInvalid": "The settings in the backup are not valid. Nothing was changed.",
    "Rejected_DatabaseInvalid": "The database in the backup failed its integrity check: {0}. Nothing was changed.",
    "ReadingBackup_Failure": "Could not read the backup file: {0}. Nothing was changed.",
    "ExtractingBackup_Failure": "Could not extract the backup: {0}. Nothing was changed.",
    "VerifyingBackup_Failure": "Could not verify the backup: {0}. Nothing was changed.",
    "PreparingSwap_Failure": "Could not prepare the restore: {0}. Nothing was changed.",
    "Swapping_Failure": "Could not replace the current data: {0}. Your previous data was put back unchanged.",
    "RollbackIncomplete_Failure": "The restore failed ({0}) and some of your previous data could not be put back automatically. It is kept at: {1}. Close InfoPhoenix and move these items back before using it again; the log has the details."
  },
  "TaskRunnerWindow": {
    "CancelButton_Caption": "Cancel",
    "CloseButton_Caption": "Close",
    "Cancelled_Message": "Operation cancelled.",
    "CloseBlocked_Message": "Please wait until the operation finishes before closing this window."
  },
  "UI": {
    "TextBlock_Settings": "Settings",
    "TextBlock_Version": "Version",
    "Badge_ComingSoon": "Coming soon",
    "Button_CheckForUpdate": "Check for update",
    "ToolTip_NotAvailableYet": "Not available yet. This feature is coming in a future version.",
    "TextBlock_Language": "Language",
    "TextBlock_ChooseLanguage": "Choose the language used across the application.",
    "TextBlock_Backup": "Backup",
    "TextBlock_CreateBackup": "Create a backup of the application data.",
    "Button_CreateBackup": "Create backup",
    "TextBlock_Restore": "Restore",
    "TextBlock_RestoreFromBackup": "Restore the application data from a backup.",
    "Button_RestoreBackup": "Restore backup",
    "TextBlock_Theme": "Theme",
    "TextBlock_ChooseTheme": "Choose the color theme used across the application.",
    "TextBlock_ConfirmBeforeExit": "Confirm before exit",
    "TextBlock_AskBeforeExit": "Ask for confirmation before closing the application.",
    "TextBlock_ApplicationDataDirectory": "Application data directory",
    "TextBlock_OpenDataDirectory": "Open the folder where the application stores its data.",
    "Button_OpenDirectory": "Open directory",
    "TextBlock_ApplicationLog": "Application log",
    "TextBlock_OpenLogFile": "Open the current application log file.",
    "Button_OpenLogFile": "Open log file",
    "TextBlock_DocumentsDirectoriesManagement": "Documents Directories Management",
    "Button_CreateNewDocumentDirectory": "Create new Document Directory",
    "Label_Label": "Label",
    "Label_DirectoryPath": "Directory Path",
    "Label_Position": "Position",
    "Button_Save": "Save",
    "Button_Close": "Close"
  }
}
```

## Appendix B: `Resources/loc/pt-BR.json` (binding content)

The Portuguese text is copied verbatim from `Localization.pt-BR.resx`. It has the same sections and keys as appendix A.

```json
{
  "AppConfigurationPageViewModel": {
    "InvalidSettings_Title": "Configurações redefinidas",
    "InvalidSettings_Message": "Não foi possível ler as configurações salvas. Elas foram redefinidas para os valores padrão.",
    "Backup_TaskRunnerWindow_Title": "Backup dos dados da aplicação",
    "Backup_Success_Message": "Backup criado: {0}",
    "Backup_Failure_Message": "O backup falhou inesperadamente: {0}",
    "Restore_SelectFile_Title": "Selecione um backup para restaurar",
    "Restore_SelectFile_FilterName": "Backups do InfoPhoenix",
    "Restore_ConfirmOverwrite_Title": "Restaurar backup",
    "Restore_ConfirmOverwrite_Message": "Restaurar \"{0}\" substituirá todos os dados atuais da aplicação (banco de dados, índice de pesquisa e configurações) pelo conteúdo deste backup.\n\nOs dados atuais serão sobrescritos e não poderão ser recuperados, a menos que você tenha feito um backup deles.\n\nDeseja continuar?",
    "Restore_Failure_Message": "A restauração falhou inesperadamente: {0}",
    "Restore_TaskRunnerWindow_Title": "Restaurar os dados da aplicação",
    "Restore_Relaunch_Title": "Restauração concluída",
    "Restore_Relaunch_Message": "Seu backup foi restaurado.\n\nFeche e abra novamente o InfoPhoenix para começar a usar os dados restaurados. Até lá, algumas configurações e informações na tela podem estar desatualizadas."
  },
  "DocumentDirectoryManagementPageViewModel": {},
  "DocumentDirectoryWindowViewModel": {
    "FileSystemDialog_Title": "Selecione um diretório de documentos"
  },
  "DocumentDirectoryWindowViewModelValidator": {
    "Label_Empty_Error_Message": "O rótulo é obrigatório.",
    "DirectoryPath_Empty_Error_Message": "O caminho do diretório é obrigatório.",
    "DirectoryPath_MaximumLength_Error_Message": "O caminho do diretório é muito longo.",
    "DirectoryPath_Unique_Error_Message": "Este diretório já está cadastrado.",
    "DisplayPosition_LowerOrEqualTo_Error_Message": "A posição de exibição não é válida."
  },
  "EntityFrameworkCoreExecuteMigrationStep": {
    "DisplayName": "Migração da Base de Dados da Aplicação",
    "ExecuteAsync_Starting": "Iniciando a migração da base de dados da aplicação...",
    "ExecuteAsync_Finished": "Migração da Base de Dados da Aplicação concluída com sucesso!"
  },
  "GetDocumentDirectoryRequestHandler": {
    "HandleAsync_MissingDocumentDirectory": "Diretório de documentos não encontrado ({0})."
  },
  "MainWindow": {
    "ConfirmApplicationExit_MessageBox_Title": "Sair da aplicação",
    "ConfirmApplicationExit_MessageBox_Message": "Tem certeza de que deseja sair? Escolha \"Não\" para parar de perguntar."
  },
  "OpenCurrentLogFileRequestHandler": {
    "Missing_Log_File_Title": "Arquivo de log não encontrado",
    "Missing_Log_File_Message": "Nenhum arquivo de log da aplicação foi encontrado ainda."
  },
  "PerformApplicationBackupRequestHandler": {
    "Preparing": "Preparando o backup...",
    "CopyingConfiguration": "Salvando e copiando as configurações...",
    "CopyingDatabase": "Copiando o banco de dados...",
    "CopyingSearchIndex": "Copiando o índice de pesquisa...",
    "CreatingArchive": "Criando o arquivo de backup...",
    "Success": "Backup criado: {0}",
    "ConfigurationMissing_Warning": "Arquivo de configurações não encontrado; o backup continua sem ele.",
    "DatabaseMissing_Warning": "Banco de dados não encontrado; o backup continua sem ele.",
    "SearchIndexMissing_Warning": "Índice de pesquisa não encontrado; o backup continua sem ele.",
    "Preparing_Failure": "O backup falhou na preparação: {0}",
    "CopyingConfiguration_Failure": "O backup falhou ao copiar as configurações: {0}",
    "CopyingDatabase_Failure": "O backup falhou ao copiar o banco de dados: {0}",
    "CopyingSearchIndex_Failure": "O backup falhou ao copiar o índice de pesquisa: {0}",
    "CreatingArchive_Failure": "O backup falhou ao criar o arquivo de backup: {0}"
  },
  "PerformApplicationRestoreRequestHandler": {
    "ReadingBackup": "Verificando o arquivo de backup...",
    "ExtractingBackup": "Extraindo o backup para uma pasta temporária...",
    "VerifyingBackup": "Verificando os dados extraídos...",
    "PreparingSwap": "Preparando a substituição dos dados atuais...",
    "Swapping": "Substituindo os dados atuais...",
    "Success": "Restauração concluída. Reinicie o InfoPhoenix para usar os dados restaurados.",
    "VersionDiffers_Warning": "Este backup foi criado pelo InfoPhoenix {0}; você está usando a versão {1}. O formato dos dados é compatível, então a restauração continua.",
    "DatabaseAbsent_Warning": "Este backup não tem banco de dados; o banco de dados atual será removido.",
    "SearchIndexAbsent_Warning": "Este backup não tem índice de pesquisa; o índice de pesquisa atual será removido.",
    "ConfigurationAbsent_Warning": "Este backup não tem configurações; suas configurações voltarão ao padrão.",
    "CleanupIncomplete_Warning": "Não foi possível excluir os dados anteriores em {0}. Você pode excluí-los manualmente.",
    "Rejected_SelectedFileInDataFolder": "O arquivo selecionado está dentro das pastas de dados do InfoPhoenix. Mova-o para outro local (por exemplo, a pasta de backups) e tente novamente. Nada foi alterado.",
    "Rejected_NotAnArchive": "O arquivo selecionado não é um arquivo de backup válido. Nada foi alterado.",
    "Rejected_ManifestMissing": "O arquivo selecionado não é um backup do InfoPhoenix. Nada foi alterado.",
    "Rejected_ManifestInvalid": "As informações do backup no arquivo selecionado estão ilegíveis ou incompletas. Nada foi alterado.",
    "Rejected_WrongApplication": "O arquivo selecionado é um backup de outra aplicação ({0}). Nada foi alterado.",
    "Rejected_UnsupportedFormat": "Este backup usa a versão de formato {0}, que esta versão do InfoPhoenix não consegue ler. Nada foi alterado.",
    "Rejected_SchemaMismatch": "A versão do banco de dados deste backup ({0}) não corresponde a esta versão do InfoPhoenix ({1}). Restaurá-lo não é suportado. Nada foi alterado.",
    "Rejected_UnexpectedEntry": "O backup contém um item inesperado ou inseguro: {0}. Nada foi alterado.",
    "Rejected_TooManyEntries": "O backup contém muitos itens ({0}, máximo {1}). Nada foi alterado.",
    "Rejected_ContentsMismatch": "O conteúdo do backup não corresponde à sua descrição; o arquivo pode estar danificado. Nada foi alterado.",
    "Rejected_InsufficientSpace": "Não há espaço livre suficiente em disco para restaurar este backup ({0} necessários). Nada foi alterado.",
    "Rejected_Corrupted": "O arquivo de backup está danificado: {0}. Nada foi alterado.",
    "Rejected_ConfigurationInvalid": "As configurações do backup não são válidas. Nada foi alterado.",
    "Rejected_DatabaseInvalid": "O banco de dados do backup falhou na verificação de integridade: {0}. Nada foi alterado.",
    "ReadingBackup_Failure": "Não foi possível ler o arquivo de backup: {0}. Nada foi alterado.",
    "ExtractingBackup_Failure": "Não foi possível extrair o backup: {0}. Nada foi alterado.",
    "VerifyingBackup_Failure": "Não foi possível verificar o backup: {0}. Nada foi alterado.",
    "PreparingSwap_Failure": "Não foi possível preparar a restauração: {0}. Nada foi alterado.",
    "Swapping_Failure": "Não foi possível substituir os dados atuais: {0}. Seus dados anteriores foram restabelecidos sem alterações.",
    "RollbackIncomplete_Failure": "A restauração falhou ({0}) e parte dos seus dados anteriores não pôde ser restabelecida automaticamente. Eles estão guardados em: {1}. Feche o InfoPhoenix e mova esses itens de volta antes de usá-lo novamente; o log tem os detalhes."
  },
  "TaskRunnerWindow": {
    "CancelButton_Caption": "Cancelar",
    "CloseButton_Caption": "Fechar",
    "Cancelled_Message": "Operação cancelada.",
    "CloseBlocked_Message": "Aguarde a conclusão da operação antes de fechar esta janela."
  },
  "UI": {
    "TextBlock_Settings": "Configurações",
    "TextBlock_Version": "Versão",
    "Badge_ComingSoon": "Em breve",
    "Button_CheckForUpdate": "Verificar atualização",
    "ToolTip_NotAvailableYet": "Ainda não disponível. Este recurso virá em uma versão futura.",
    "TextBlock_Language": "Idioma",
    "TextBlock_ChooseLanguage": "Escolha o idioma utilizado em toda a aplicação.",
    "TextBlock_Backup": "Cópia de segurança",
    "TextBlock_CreateBackup": "Cria uma cópia de segurança dos dados da aplicação.",
    "Button_CreateBackup": "Criar cópia de segurança",
    "TextBlock_Restore": "Restauração",
    "TextBlock_RestoreFromBackup": "Restaura os dados da aplicação a partir de uma cópia de segurança.",
    "Button_RestoreBackup": "Restaurar cópia de segurança",
    "TextBlock_Theme": "Tema",
    "TextBlock_ChooseTheme": "Escolha o tema de cores utilizado em toda a aplicação.",
    "TextBlock_ConfirmBeforeExit": "Confirmar antes de sair",
    "TextBlock_AskBeforeExit": "Pedir confirmação antes de fechar a aplicação.",
    "TextBlock_ApplicationDataDirectory": "Diretório de dados da aplicação",
    "TextBlock_OpenDataDirectory": "Abrir a pasta onde a aplicação guarda seus dados.",
    "Button_OpenDirectory": "Abrir diretório",
    "TextBlock_ApplicationLog": "Log da aplicação",
    "TextBlock_OpenLogFile": "Abrir o arquivo de log atual da aplicação.",
    "Button_OpenLogFile": "Abrir arquivo de log",
    "TextBlock_DocumentsDirectoriesManagement": "Gerenciador de Diretórios de Documentos",
    "Button_CreateNewDocumentDirectory": "Criar novo Diretório de Documentos",
    "Label_Label": "Rótulo",
    "Label_DirectoryPath": "Caminho do Diretório",
    "Label_Position": "Posição",
    "Button_Save": "Salvar",
    "Button_Close": "Fechar"
  }
}
```

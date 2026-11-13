using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;
using Nameless.Windows.Localization;
using Nameless.Windows.Localization.Json;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Pages;

/// <summary>
/// Fitness functions F1-F6 (plan IPX-0015 section 4): retargeted from the resx-backed
/// <c>Localization.ResourceManager</c> to the JSON files <c>JsonTranslator</c> actually reads at
/// runtime (D1/D3). The resx check these replaced proved nothing about the runtime translator -
/// the resx files are no longer the source of truth for the keys covered here (D5).
/// </summary>
public class LocalizationFitnessTests {
    private static readonly string[] CultureFiles = ["default.json", "pt-BR.json"];

    // ----- F1/B7: every (section, key) pair is present, non-empty and differs -------------------

    // Pinned from plan IPX-0015 appendix A/B (106 entries across 12 sections) - fixed, independent
    // of whatever the deployed JSON files currently contain. This is what lets this theory (and
    // RequiredKeys_HaveNoExtraEntries_InEitherCultureFile below) actually catch a key silently
    // dropped from both files, instead of just re-checking the files against themselves.
    private static readonly (string Section, string Key)[] RequiredSectionKeyPairs = [
        ("AppConfigurationPageViewModel", "InvalidSettings_Title"),
        ("AppConfigurationPageViewModel", "InvalidSettings_Message"),
        ("AppConfigurationPageViewModel", "Backup_TaskRunnerWindow_Title"),
        ("AppConfigurationPageViewModel", "Backup_Success_Message"),
        ("AppConfigurationPageViewModel", "Backup_Failure_Message"),
        ("AppConfigurationPageViewModel", "Restore_SelectFile_Title"),
        ("AppConfigurationPageViewModel", "Restore_SelectFile_FilterName"),
        ("AppConfigurationPageViewModel", "Restore_ConfirmOverwrite_Title"),
        ("AppConfigurationPageViewModel", "Restore_ConfirmOverwrite_Message"),
        ("AppConfigurationPageViewModel", "Restore_Failure_Message"),
        ("AppConfigurationPageViewModel", "Restore_TaskRunnerWindow_Title"),
        ("AppConfigurationPageViewModel", "Restore_Relaunch_Title"),
        ("AppConfigurationPageViewModel", "Restore_Relaunch_Message"),
        ("DocumentDirectoryWindowViewModel", "FileSystemDialog_Title"),
        ("DocumentDirectoryWindowViewModelValidator", "Label_Empty_Error_Message"),
        ("DocumentDirectoryWindowViewModelValidator", "DirectoryPath_Empty_Error_Message"),
        ("DocumentDirectoryWindowViewModelValidator", "DirectoryPath_MaximumLength_Error_Message"),
        ("DocumentDirectoryWindowViewModelValidator", "DirectoryPath_Unique_Error_Message"),
        ("DocumentDirectoryWindowViewModelValidator", "DisplayPosition_LowerOrEqualTo_Error_Message"),
        ("EntityFrameworkCoreExecuteMigrationStep", "DisplayName"),
        ("EntityFrameworkCoreExecuteMigrationStep", "ExecuteAsync_Starting"),
        ("EntityFrameworkCoreExecuteMigrationStep", "ExecuteAsync_Finished"),
        ("GetDocumentDirectoryRequestHandler", "HandleAsync_MissingDocumentDirectory"),
        ("MainWindow", "ConfirmApplicationExit_MessageBox_Title"),
        ("MainWindow", "ConfirmApplicationExit_MessageBox_Message"),
        ("OpenCurrentLogFileRequestHandler", "Missing_Log_File_Title"),
        ("OpenCurrentLogFileRequestHandler", "Missing_Log_File_Message"),
        ("PerformApplicationBackupRequestHandler", "Preparing"),
        ("PerformApplicationBackupRequestHandler", "CopyingConfiguration"),
        ("PerformApplicationBackupRequestHandler", "CopyingDatabase"),
        ("PerformApplicationBackupRequestHandler", "CopyingSearchIndex"),
        ("PerformApplicationBackupRequestHandler", "CreatingArchive"),
        ("PerformApplicationBackupRequestHandler", "Success"),
        ("PerformApplicationBackupRequestHandler", "ConfigurationMissing_Warning"),
        ("PerformApplicationBackupRequestHandler", "DatabaseMissing_Warning"),
        ("PerformApplicationBackupRequestHandler", "SearchIndexMissing_Warning"),
        ("PerformApplicationBackupRequestHandler", "Preparing_Failure"),
        ("PerformApplicationBackupRequestHandler", "CopyingConfiguration_Failure"),
        ("PerformApplicationBackupRequestHandler", "CopyingDatabase_Failure"),
        ("PerformApplicationBackupRequestHandler", "CopyingSearchIndex_Failure"),
        ("PerformApplicationBackupRequestHandler", "CreatingArchive_Failure"),
        ("PerformApplicationRestoreRequestHandler", "ReadingBackup"),
        ("PerformApplicationRestoreRequestHandler", "ExtractingBackup"),
        ("PerformApplicationRestoreRequestHandler", "VerifyingBackup"),
        ("PerformApplicationRestoreRequestHandler", "PreparingSwap"),
        ("PerformApplicationRestoreRequestHandler", "Swapping"),
        ("PerformApplicationRestoreRequestHandler", "Success"),
        ("PerformApplicationRestoreRequestHandler", "VersionDiffers_Warning"),
        ("PerformApplicationRestoreRequestHandler", "DatabaseAbsent_Warning"),
        ("PerformApplicationRestoreRequestHandler", "SearchIndexAbsent_Warning"),
        ("PerformApplicationRestoreRequestHandler", "ConfigurationAbsent_Warning"),
        ("PerformApplicationRestoreRequestHandler", "CleanupIncomplete_Warning"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_SelectedFileInDataFolder"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_NotAnArchive"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_ManifestMissing"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_ManifestInvalid"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_WrongApplication"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_UnsupportedFormat"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_SchemaMismatch"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_UnexpectedEntry"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_TooManyEntries"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_ContentsMismatch"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_InsufficientSpace"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_Corrupted"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_ConfigurationInvalid"),
        ("PerformApplicationRestoreRequestHandler", "Rejected_DatabaseInvalid"),
        ("PerformApplicationRestoreRequestHandler", "ReadingBackup_Failure"),
        ("PerformApplicationRestoreRequestHandler", "ExtractingBackup_Failure"),
        ("PerformApplicationRestoreRequestHandler", "VerifyingBackup_Failure"),
        ("PerformApplicationRestoreRequestHandler", "PreparingSwap_Failure"),
        ("PerformApplicationRestoreRequestHandler", "Swapping_Failure"),
        ("PerformApplicationRestoreRequestHandler", "RollbackIncomplete_Failure"),
        ("TaskRunnerWindow", "CancelButton_Caption"),
        ("TaskRunnerWindow", "CloseButton_Caption"),
        ("TaskRunnerWindow", "Cancelled_Message"),
        ("TaskRunnerWindow", "CloseBlocked_Message"),
        ("UI", "TextBlock_Settings"),
        ("UI", "TextBlock_Version"),
        ("UI", "Badge_ComingSoon"),
        ("UI", "Button_CheckForUpdate"),
        ("UI", "ToolTip_NotAvailableYet"),
        ("UI", "TextBlock_Language"),
        ("UI", "TextBlock_ChooseLanguage"),
        ("UI", "TextBlock_Backup"),
        ("UI", "TextBlock_CreateBackup"),
        ("UI", "Button_CreateBackup"),
        ("UI", "TextBlock_Restore"),
        ("UI", "TextBlock_RestoreFromBackup"),
        ("UI", "Button_RestoreBackup"),
        ("UI", "TextBlock_Theme"),
        ("UI", "TextBlock_ChooseTheme"),
        ("UI", "TextBlock_ConfirmBeforeExit"),
        ("UI", "TextBlock_AskBeforeExit"),
        ("UI", "TextBlock_ApplicationDataDirectory"),
        ("UI", "TextBlock_OpenDataDirectory"),
        ("UI", "Button_OpenDirectory"),
        ("UI", "TextBlock_ApplicationLog"),
        ("UI", "TextBlock_OpenLogFile"),
        ("UI", "Button_OpenLogFile"),
        ("UI", "TextBlock_DocumentsDirectoriesManagement"),
        ("UI", "Button_CreateNewDocumentDirectory"),
        ("UI", "Label_Label"),
        ("UI", "Label_DirectoryPath"),
        ("UI", "Label_Position"),
        ("UI", "Button_Save"),
        ("UI", "Button_Close"),
    ];

    public static TheoryData<string, string> SectionKeyPairs() {
        var data = new TheoryData<string, string>();

        foreach (var (section, key) in RequiredSectionKeyPairs) {
            data.Add(section, key);
        }

        return data;
    }

    [Fact]
    public void RequiredKeys_HaveNoExtraEntries_InEitherCultureFile() {
        var (defaultSections, ptBrSections) = LoadBothFiles();
        var required = RequiredSectionKeyPairs.ToHashSet();

        AssertNoExtraKeys("default.json", defaultSections, required);
        AssertNoExtraKeys("pt-BR.json", ptBrSections, required);
    }

    private static void AssertNoExtraKeys(
        string fileName,
        Dictionary<string, Dictionary<string, string>> sections,
        HashSet<(string Section, string Key)> required) {
        foreach (var (sectionName, entries) in sections) {
            foreach (var key in entries.Keys) {
                Assert.True(
                    required.Contains((sectionName, key)),
                    $"'{fileName}' has '{sectionName}.{key}', which is not in the pinned required-key list (plan appendix A/B). " +
                    "Add it there, or remove it if it's dead.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(SectionKeyPairs))]
    public void Key_IsPresentNonEmptyAndDistinct_InBothCultureFiles(string section, string key) {
        var (defaultSections, ptBrSections) = LoadBothFiles();

        var defaultValue = defaultSections.TryGetValue(section, out var defaultEntries) ? defaultEntries.GetValueOrDefault(key) : null;
        var ptBrValue = ptBrSections.TryGetValue(section, out var ptBrEntries) ? ptBrEntries.GetValueOrDefault(key) : null;

        Assert.True(defaultValue is not null, $"default.json is missing section '{section}' key '{key}'.");
        Assert.True(ptBrValue is not null, $"pt-BR.json is missing section '{section}' key '{key}'.");

        Assert.False(string.IsNullOrWhiteSpace(defaultValue), $"default.json has an empty value for '{section}.{key}'.");
        Assert.False(string.IsNullOrWhiteSpace(ptBrValue), $"pt-BR.json has an empty value for '{section}.{key}'.");
        Assert.NotEqual(defaultValue, ptBrValue);
    }

    // ----- F2/B8: placeholder parity, no stray non-{digit} braces ---------------------------------

    private static readonly Regex PlaceholderPattern = new(@"\{(\d+)\}", RegexOptions.Compiled);

    [Theory]
    [MemberData(nameof(SectionKeyPairs))]
    public void Key_PlaceholdersMatchBetweenCultures_AndHaveNoStrayBraces(string section, string key) {
        var (defaultSections, ptBrSections) = LoadBothFiles();

        if (!defaultSections.TryGetValue(section, out var defaultEntries) || !defaultEntries.TryGetValue(key, out var defaultValue) ||
            !ptBrSections.TryGetValue(section, out var ptBrEntries) || !ptBrEntries.TryGetValue(key, out var ptBrValue)) {
            return; // Already reported as missing by the presence test above.
        }

        var defaultPlaceholders = ExtractPlaceholders(defaultValue);
        var ptBrPlaceholders = ExtractPlaceholders(ptBrValue);

        Assert.True(
            defaultPlaceholders.SetEquals(ptBrPlaceholders),
            $"Placeholder sets differ for '{section}.{key}': default has [{string.Join(",", defaultPlaceholders)}], pt-BR has [{string.Join(",", ptBrPlaceholders)}].");

        AssertNoStrayBraces(section, key, "default", defaultValue);
        AssertNoStrayBraces(section, key, "pt-BR", ptBrValue);
    }

    private static HashSet<string> ExtractPlaceholders(string value) {
        return PlaceholderPattern.Matches(value).Select(m => m.Groups[1].Value).ToHashSet();
    }

    private static void AssertNoStrayBraces(string section, string key, string culture, string value) {
        // A lone unescaped '{' or '}' (not part of a balanced "{digits}" placeholder) would never
        // match AnyBracePattern's old balanced-brace shape, so strip every valid placeholder first
        // and check what's left for any remaining brace character at all (plan D4 / review 🟡 #3).
        var withoutPlaceholders = PlaceholderPattern.Replace(value, string.Empty);

        Assert.True(
            !withoutPlaceholders.Contains('{') && !withoutPlaceholders.Contains('}'),
            $"'{culture}.json' entry '{section}.{key}' has a brace that is not a bare {{digits}} placeholder: '{value}'. " +
            "A literal '{' reaches string.Format at lookup time and throws FormatException (plan D4).");
    }

    // ----- F3/B9: composed key uniqueness within each file -----------------------------------------

    [Fact]
    public void ComposedKeys_AreUnique_WithinEachCultureFile() {
        var (defaultSections, ptBrSections) = LoadBothFiles();

        AssertComposedKeysUnique("default.json", defaultSections);
        AssertComposedKeysUnique("pt-BR.json", ptBrSections);
    }

    private static void AssertComposedKeysUnique(string fileName, Dictionary<string, Dictionary<string, string>> sections) {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (sectionName, entries) in sections) {
            foreach (var key in entries.Keys) {
                var composed = TranslationKey.Compose(sectionName, key);

                Assert.True(seen.Add(composed), $"'{fileName}' has a composed-key collision at '{composed}' (section '{sectionName}').");
            }
        }
    }

    // ----- F3b/B10: UI key naming rule + identical UI key sets -------------------------------------

    private static readonly Regex UiKeyPattern = new(
        @"^([A-Za-z]+_)?(TextBlock|Button|Label|Badge|ToolTip|MessageBox)_[A-Za-z]+$", RegexOptions.Compiled);

    [Fact]
    public void UiSectionKeys_MatchTheComponentNamingRule_AndAreIdenticalAcrossBothFiles() {
        var (defaultSections, ptBrSections) = LoadBothFiles();

        Assert.True(defaultSections.TryGetValue("UI", out var defaultUi), "default.json has no 'UI' section.");
        Assert.True(ptBrSections.TryGetValue("UI", out var ptBrUi), "pt-BR.json has no 'UI' section.");

        foreach (var key in defaultUi.Keys) {
            Assert.True(UiKeyPattern.IsMatch(key), $"UI key '{key}' (default.json) does not match the naming rule.");
        }

        foreach (var key in ptBrUi.Keys) {
            Assert.True(UiKeyPattern.IsMatch(key), $"UI key '{key}' (pt-BR.json) does not match the naming rule.");
        }

        Assert.Equal(
            defaultUi.Keys.OrderBy(k => k, StringComparer.Ordinal),
            ptBrUi.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    // ----- F3c/B11: every UI_* XAML key exists in both JSON files ----------------------------------

    private static readonly Regex XamlTranslatorKeyPattern = new(@"\{loc:Translator\s+([A-Za-z0-9_]+)\}", RegexOptions.Compiled);

    [Fact]
    public void EveryUiKeyReferencedFromXaml_ExistsInBothCultureFiles() {
        var (defaultSections, ptBrSections) = LoadBothFiles();
        var composedKeys = ComposeAllKeys(defaultSections);
        var ptBrComposedKeys = ComposeAllKeys(ptBrSections);

        var viewsPath = Path.Combine(GetRepositoryRootPath(), "src", "Nameless.InfoPhoenix.Client", "Views");
        Assert.True(Directory.Exists(viewsPath), $"Expected the source Views folder to exist at '{viewsPath}'.");

        var referencedUiKeys = Directory.EnumerateFiles(viewsPath, "*.xaml", SearchOption.AllDirectories)
            .SelectMany(path => XamlTranslatorKeyPattern.Matches(File.ReadAllText(path)).Select(m => m.Groups[1].Value))
            .Where(key => key.StartsWith("UI_", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(referencedUiKeys); // Sanity: the scan itself must actually find something.

        foreach (var key in referencedUiKeys) {
            Assert.Contains(key, composedKeys);
            Assert.Contains(key, ptBrComposedKeys);
        }
    }

    private static HashSet<string> ComposeAllKeys(Dictionary<string, Dictionary<string, string>> sections) {
        var composed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (sectionName, entries) in sections) {
            foreach (var key in entries.Keys) {
                composed.Add(TranslationKey.Compose(sectionName, key));
            }
        }

        return composed;
    }

    // ----- F4/B12: appsettings.json wires a real, existing RootPath ---------------------------------

    [Fact]
    public void Appsettings_HasANonEmptyTranslatorRootPath_AndBothCultureFilesExistThere() {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var rootPath = configuration["Translator:RootPath"];

        Assert.False(string.IsNullOrWhiteSpace(rootPath), "Translator:RootPath must be set (plan D1 - the startup-crash regression test).");

        foreach (var fileName in CultureFiles) {
            var resolvedPath = Path.Combine(AppContext.BaseDirectory, rootPath!, fileName);
            Assert.True(File.Exists(resolvedPath), $"Expected '{resolvedPath}' to exist.");
        }
    }

    // ----- F5/B13: a REAL JsonTranslator resolves real keys for real classes ------------------------

    [Fact]
    public void RealJsonTranslator_ResolvesUiTaskRunnerAndHandlerKeys_WithoutMissingKeyPlaceholders() {
        var rootPath = Path.Combine(AppContext.BaseDirectory, "Resources", "loc");
        var options = Options.Create(new TranslatorOptions { RootPath = rootPath });

        using var fileProvider = new FileSystemTranslationFileProvider(options, NullLogger<FileSystemTranslationFileProvider>.Instance);
        using var translator = new JsonTranslator(fileProvider, NullLogger<JsonTranslator>.Instance);

        // No SetCulture call (plan F5): mutating CultureInfo.CurrentCulture/CurrentUICulture
        // process-wide from a test is unacceptable - either active culture must resolve real text.
        AssertResolved(translator["UI_TextBlock_Settings"]);
        AssertResolved(translator["TaskRunnerWindow_CancelButton_Caption"]);

        var factory = new TranslatorFactory(translator);
        var handlerTranslator = factory.Create<PerformApplicationBackupRequestHandler>();
        AssertResolved(handlerTranslator["Preparing"]);
    }

    private static void AssertResolved(string value) {
        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.False(value.StartsWith('['), $"Expected a resolved value, got the missing-key placeholder '{value}'.");
    }

    // ----- F6/B14: no public constructor takes the raw, non-generic ITranslator (except views) -----

    [Fact]
    public void NoPublicConstructor_TakesTheRawNonGenericITranslator_ExceptWindowPageOrUserControlTypes() {
        var assembly = typeof(App).Assembly;
        var offenders = new List<string>();

        foreach (var type in assembly.GetTypes()) {
            if (IsExemptView(type)) {
                continue;
            }

            foreach (var constructor in type.GetConstructors()) {
                if (constructor.GetParameters().Any(p => p.ParameterType == typeof(ITranslator))) {
                    offenders.Add($"{type.FullName}.{constructor}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static bool IsExemptView(Type type) {
        return typeof(System.Windows.Window).IsAssignableFrom(type) ||
            typeof(System.Windows.Controls.Page).IsAssignableFrom(type) ||
            typeof(System.Windows.Controls.UserControl).IsAssignableFrom(type);
    }

    // ----- helpers ----------------------------------------------------------------------------------

    private static (Dictionary<string, Dictionary<string, string>> Default, Dictionary<string, Dictionary<string, string>> PtBr) LoadBothFiles() {
        var rootPath = Path.Combine(AppContext.BaseDirectory, "Resources", "loc");

        return (
            LoadFile(Path.Combine(rootPath, "default.json")),
            LoadFile(Path.Combine(rootPath, "pt-BR.json")));
    }

    // Mirrors JsonTranslator's own strict parse options (plan D1/§4): a duplicate property
    // anywhere in the file must fail loudly here too, not be silently resolved by keeping the
    // last value the way the default JsonSerializerOptions would.
    private static readonly JsonSerializerOptions StrictParseOptions = new() { AllowDuplicateProperties = false };

    private static Dictionary<string, Dictionary<string, string>> LoadFile(string path) {
        Assert.True(File.Exists(path), $"Expected '{path}' to exist.");

        var json = File.ReadAllText(path);
        var sections = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, StrictParseOptions);

        Assert.NotNull(sections);

        return sections;
    }

    private static string GetRepositoryRootPath([CallerFilePath] string? testFilePath = null) {
        // This file lives at tests/Nameless.InfoPhoenix.Client.Tests/Views/Pages/*.cs - four
        // levels up from its own compile-time path is the repository root, regardless of
        // whatever the test runner's current or output directory happens to be.
        var directory = Path.GetDirectoryName(testFilePath)!;

        return Path.GetFullPath(Path.Combine(directory, "..", "..", "..", ".."));
    }
}
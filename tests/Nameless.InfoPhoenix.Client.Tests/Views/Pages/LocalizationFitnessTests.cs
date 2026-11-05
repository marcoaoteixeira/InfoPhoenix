using System.Globalization;
using Nameless.InfoPhoenix.Client.Resources.l10n;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Pages;

/// <summary>
/// Fitness function F2 (plan §4): every <c>{loc:Translator Key}</c> key referenced by
/// <c>AppConfigurationPage.xaml</c>, the closing confirmation dialog
/// (<c>MainWindow</c>) and <c>OpenCurrentLogFileRequestHandler</c>'s warning must resolve to a
/// non-empty string for both the neutral (English) and pt-BR resources, and the pt-BR value
/// must differ from the neutral one - catching a missing translation before it shows up as a
/// raw <c>[Key]</c> placeholder at runtime.
/// </summary>
public class LocalizationFitnessTests {
    public static TheoryData<string> Keys => new() {
        "AppConfigurationPage_Title",
        "AppConfigurationPage_VersionCard_Title",
        "AppConfigurationPage_ApplicationLanguageCard_Title",
        "AppConfigurationPage_ApplicationLanguageCard_Message",
        "AppConfigurationPage_VersionCard_Button_Caption",
        "AppConfigurationPage_ApplicationBackupCard_Title",
        "AppConfigurationPage_ApplicationBackupCard_Message",
        "AppConfigurationPage_ApplicationBackupCard_Button_Caption",
        "AppConfigurationPage_ApplicationRestoreCard_Title",
        "AppConfigurationPage_ApplicationRestoreCard_Message",
        "AppConfigurationPage_ApplicationRestoreCard_Button_Caption",
        "AppConfigurationPage_ComingSoon_Badge",
        "AppConfigurationPage_ComingSoon_Tooltip",
        "AppConfigurationPage_ThemeCard_Title",
        "AppConfigurationPage_ThemeCard_Message",
        "AppConfigurationPage_ExitBehaviorCard_Title",
        "AppConfigurationPage_ExitBehaviorCard_Message",
        "AppConfigurationPage_AppDirectoryPathCard_Title",
        "AppConfigurationPage_AppDirectoryPathCard_Message",
        "AppConfigurationPage_AppDirectoryPathCard_Button_Caption",
        "AppConfigurationPage_ApplicationLogCard_Title",
        "AppConfigurationPage_ApplicationLogCard_Message",
        "AppConfigurationPage_ApplicationLogCard_Button_Caption",
        "MainWindow_ConfirmApplicationExit_MessageBox_Title",
        "MainWindow_ConfirmApplicationExit_MessageBox_Message",
        "OpenCurrentLogFileRequestHandler_Missing_Log_File_Title",
        "OpenCurrentLogFileRequestHandler_Missing_Log_File_Message",
        "AppConfigurationPage_InvalidSettings_Title",
        "AppConfigurationPage_InvalidSettings_Message",
    };

    [Theory]
    [MemberData(nameof(Keys))]
    public void Key_ResolvesToNonEmptyAndDistinctValues_ForNeutralAndPtBr(string key) {
        var resourceManager = Localization.ResourceManager;

        var neutral = resourceManager.GetString(key, CultureInfo.InvariantCulture);
        var ptBr = resourceManager.GetString(key, CultureInfo.GetCultureInfo("pt-BR"));

        Assert.False(string.IsNullOrWhiteSpace(neutral), $"Missing neutral (English) value for '{key}'.");
        Assert.False(string.IsNullOrWhiteSpace(ptBr), $"Missing pt-BR value for '{key}'.");
        Assert.NotEqual(neutral, ptBr);
    }
}

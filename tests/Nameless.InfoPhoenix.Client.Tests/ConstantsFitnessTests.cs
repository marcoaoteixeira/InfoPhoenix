using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Tests;

/// <summary>
/// F1/F2 (plan §4): <see cref="Constants.AppConfiguration"/> duplicates key names and default
/// values that <c>Nameless.Windows</c>' <c>AppConfigurationManagerExtensions</c> keeps private.
/// These tests fail at the next package bump if a key name or default changes underneath us.
/// </summary>
public class ConstantsFitnessTests {
    [Fact]
    public void ThemeKey_MatchesTheKeyTheExtensionPropertySetterWritesUnder() {
        var manager = new FakeAppConfigurationManager();

        manager.Theme = Theme.Dark;

        Assert.True(manager.TryGet<object>(Constants.AppConfiguration.Keys.Theme, out _));
    }

    [Fact]
    public void LanguageKey_MatchesTheKeyTheExtensionPropertySetterWritesUnder() {
        var manager = new FakeAppConfigurationManager();

        manager.Language = "en-GB";

        Assert.True(manager.TryGet<object>(Constants.AppConfiguration.Keys.Language, out _));
    }

    [Fact]
    public void ConfirmBeforeExitKey_MatchesTheKeyTheExtensionPropertySetterWritesUnder() {
        var manager = new FakeAppConfigurationManager();

        manager.ConfirmBeforeExit = true;

        Assert.True(manager.TryGet<object>(Constants.AppConfiguration.Keys.ConfirmBeforeExit, out _));
    }

    [Fact]
    public void EmptyManager_ThemeGetterDefault_MatchesConstantsDefault() {
        var manager = new FakeAppConfigurationManager();

        Assert.Equal(Constants.AppConfiguration.Defaults.Theme, manager.Theme);
    }

    [Fact]
    public void EmptyManager_LanguageGetterDefault_MatchesConstantsDefault() {
        var manager = new FakeAppConfigurationManager();

        Assert.Equal(Constants.AppConfiguration.Defaults.Language, manager.Language);
    }

    [Fact]
    public void EmptyManager_ConfirmBeforeExitGetterDefault_MatchesConstantsDefault() {
        var manager = new FakeAppConfigurationManager();

        Assert.Equal(Constants.AppConfiguration.Defaults.ConfirmBeforeExit, manager.ConfirmBeforeExit);
    }
}

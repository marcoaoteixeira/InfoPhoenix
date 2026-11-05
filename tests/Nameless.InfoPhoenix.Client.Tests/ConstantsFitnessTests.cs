using System.IO;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.IO.Physical;
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

    /// <summary>
    /// F4 (plan IPX-0009 §4/§3.6): saving through the real <c>AppConfigurationManager</c> must
    /// produce a file named <see cref="Constants.BackupRecovery.AppConfigurationFileName"/> -
    /// the backup handler copies that exact file name, so a package bump that renames the
    /// package's own private constant must fail here, not silently skip the config in a backup.
    /// </summary>
    [Fact]
    public async Task SavingThroughTheRealManager_ProducesAFileNamedAsTheBackupRecoveryConstant() {
        var root = Directory.CreateTempSubdirectory("ipx-0009-f4-").FullName;

        try {
            var applicationContext = new FakeApplicationContext {
                FileManager = new PhysicalFileManagerFactory().GetOrCreate(root)
            };
            var manager = new AppConfigurationManager(
                applicationContext,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AppConfigurationManager>.Instance);

            manager.Theme = Theme.Dark;
            await manager.SaveChangesAsync(CancellationToken.None);

            var expectedPath = Path.Combine(root, Constants.BackupRecovery.AppConfigurationFileName);
            Assert.True(File.Exists(expectedPath), $"Expected '{expectedPath}' to exist.");
        }
        finally {
            Directory.Delete(root, recursive: true);
        }
    }
}
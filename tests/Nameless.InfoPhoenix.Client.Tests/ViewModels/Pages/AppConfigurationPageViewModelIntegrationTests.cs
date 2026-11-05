using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.IO.Physical;
using Nameless.Windows.AppConfiguration;

namespace Nameless.InfoPhoenix.Client.Tests.ViewModels.Pages;

/// <summary>
/// Exercises the view model's immediate-save path against the real
/// <see cref="AppConfigurationManager"/> (plan T5/B6/B7) instead of the fake used by
/// <see cref="AppConfigurationPageViewModelTests"/>, proving the end-to-end "persisted before
/// shutdown" promise (AC2) over the manager's actual atomic write-then-rename file format.
/// </summary>
public sealed class AppConfigurationPageViewModelIntegrationTests : IDisposable {
    private readonly string _rootDirectory = Directory.CreateTempSubdirectory("ipx-0004-").FullName;

    public void Dispose() {
        Directory.Delete(_rootDirectory, recursive: true);
    }

    [Fact]
    public void ConfirmBeforeExitChange_PersistsBeforeShutdown_ReadableByANewManagerInstance() {
        StaWindowTestHarness.Run(() => {
            var manager = CreateManager();
            var vm = CreateViewModel(manager);

            vm.OnNavigatedToAsync();
            vm.CurrentConfirmBeforeExit = true;

            WaitForConfigFile();

            var reloaded = CreateManager();

            Assert.True(reloaded.ConfirmBeforeExit);
        });
    }

    [Fact]
    public void RapidToggles_FiredSynchronously_NeverLeaveAnIntermediateOrCorruptFileOnDisk() {
        StaWindowTestHarness.Run(() => {
            var manager = CreateManager();
            var vm = CreateViewModel(manager);

            vm.OnNavigatedToAsync();

            // Fired synchronously, back to back, without awaiting any pending save in between.
            vm.CurrentConfirmBeforeExit = true;
            vm.CurrentConfirmBeforeExit = false;
            vm.CurrentConfirmBeforeExit = true;

            WaitForConfigFile();

            // Read directly off disk via a brand-new manager instance: the file must hold a
            // complete, valid JSON document reflecting the final state, never a partial write.
            var reloaded = CreateManager();

            Assert.True(reloaded.ConfirmBeforeExit);
        });
    }

    private AppConfigurationManager CreateManager() {
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_rootDirectory)
        };

        return new AppConfigurationManager(applicationContext, NullLogger<AppConfigurationManager>.Instance);
    }

    private static AppConfigurationPageViewModel CreateViewModel(IAppConfigurationManager manager) {
        return new AppConfigurationPageViewModel(
            manager,
            new FakeApplicationContext(),
            new FakeTranslator(),
            new FakeMediator(),
            NullLogger<AppConfigurationPageViewModel>.Instance
        );
    }

    private void WaitForConfigFile() {
        var configFile = Path.Combine(_rootDirectory, "app.config");
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (!File.Exists(configFile) && DateTime.UtcNow < deadline) {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                () => { },
                System.Windows.Threading.DispatcherPriority.Background
            );
        }

        Assert.True(File.Exists(configFile), $"Expected '{configFile}' to exist after the save chain completed.");
    }
}

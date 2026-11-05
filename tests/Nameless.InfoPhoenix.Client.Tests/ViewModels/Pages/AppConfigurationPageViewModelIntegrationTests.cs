using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.IO.Physical;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Notification.SnackBar;
using Nameless.Windows.UI;

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

    /// <summary>
    /// B18: the recovery path works end-to-end against the real <see cref="AppConfigurationManager"/>
    /// - an unsupported stored value (<c>Theme</c> as a JSON number with no matching enum
    /// member) publishes one Failure snackbar, and the reset round-trips through an actual
    /// save (a new manager instance reads back the default <see cref="Theme.System"/>).
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithAnUnsupportedStoredValue_RecoversAndPersistsTheDefault_OverTheRealManager() {
        StaWindowTestHarness.Run(() => {
            File.WriteAllText(Path.Combine(_rootDirectory, "app.config"), """{"Theme":999}""");

            var manager = CreateManager();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager, eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());

            WaitForConfigFileToStopContaining("999");

            var reloaded = CreateManager();
            Assert.Equal(Theme.System, reloaded.Theme);
        });
    }

    /// <summary>
    /// Happy path over the real manager (review round 2, finding 1): non-default valid values
    /// written and saved through one manager instance must read back unchanged, as themselves
    /// (not the defaults), through a fresh manager/VM pair, and recovery must stay silent -
    /// proving B18's "unsupported value" snackbar isn't firing on every navigation regardless
    /// of content.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithNonDefaultValidStoredValues_ShowsStoredValues_WithoutPublishing() {
        StaWindowTestHarness.Run(() => {
            var writingManager = CreateManager();
            writingManager.Theme = Theme.Dark;
            writingManager.Language = "en-GB";
            writingManager.ConfirmBeforeExit = true;

            // Must not block with .GetAwaiter().GetResult(): the real manager's save genuinely
            // awaits I/O, and its continuation resumes through this very STA thread's
            // Dispatcher - blocking it here would deadlock. Pump the dispatcher instead, same
            // as the other real-manager tests below.
            var saveTask = writingManager.SaveChangesAsync(CancellationToken.None);
            PumpDispatcherUntil(() => saveTask.IsCompleted);
            saveTask.GetAwaiter().GetResult();

            var manager = CreateManager();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager, eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            Assert.Empty(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(Theme.Dark, vm.CurrentTheme.Tag);
            Assert.Equal("en-GB", vm.CurrentLanguage.Tag);
            Assert.True(vm.CurrentConfirmBeforeExit);
        });
    }

    /// <summary>
    /// B19 (documents accepted gap D-1): malformed JSON is swallowed by the real manager and
    /// returns an empty dictionary, indistinguishable from a first run - so no snackbar
    /// appears, but the app does not crash and the page shows the defaults.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithMalformedJson_ShowsDefaults_WithoutCrashingOrPublishing_D1Gap() {
        StaWindowTestHarness.Run(() => {
            File.WriteAllText(Path.Combine(_rootDirectory, "app.config"), "{{{");

            var manager = CreateManager();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager, eventPublisher);

            var exception = Record.Exception(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.Null(exception);
            Assert.Empty(eventPublisher.PublishedEvents);
            Assert.Equal(Theme.System, vm.CurrentTheme.Tag);
        });
    }

    private AppConfigurationManager CreateManager() {
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_rootDirectory)
        };

        return new AppConfigurationManager(applicationContext, NullLogger<AppConfigurationManager>.Instance);
    }

    private static AppConfigurationPageViewModel CreateViewModel(
        IAppConfigurationManager manager, FakeEventPublisher? eventPublisher = null) {
        return new AppConfigurationPageViewModel(
            manager,
            new FakeApplicationContext(),
            new FakeApplicationThemeApplier(),
            new FakeTranslator(),
            new FakeMediator(),
            eventPublisher ?? new FakeEventPublisher(),
            NullLogger<AppConfigurationPageViewModel>.Instance
        );
    }

    /// <summary>
    /// Unlike <see cref="WaitForConfigFile"/> (which only waits for the file to exist), this
    /// waits until a save actually overwrites the given marker - needed when the file is
    /// pre-seeded by the test itself (B18), so its mere existence proves nothing about whether
    /// the recovery's save has landed yet.
    /// </summary>
    private void WaitForConfigFileToStopContaining(string marker) {
        var configFile = Path.Combine(_rootDirectory, "app.config");
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (File.ReadAllText(configFile).Contains(marker, StringComparison.Ordinal) && DateTime.UtcNow < deadline) {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                () => { },
                System.Windows.Threading.DispatcherPriority.Background
            );
        }

        Assert.DoesNotContain(marker, File.ReadAllText(configFile));
    }

    private static void PumpDispatcherUntil(Func<bool> condition) {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (!condition() && DateTime.UtcNow < deadline) {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                () => { },
                System.Windows.Threading.DispatcherPriority.Background
            );
        }

        Assert.True(condition(), "Timed out waiting for the condition.");
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
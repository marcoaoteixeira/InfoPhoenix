using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Features.OpenCurrentLogFile;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Tests.ViewModels.Pages;

public class AppConfigurationPageViewModelTests {
    [Fact]
    public void OnNavigatedToAsync_ReadsBackThemeChangeImmediately_WithExactlyOneSave() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var vm = CreateViewModel(manager: manager);

            vm.OnNavigatedToAsync();

            var light = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.Light));
            vm.CurrentTheme = light;

            Assert.Equal(Theme.Light, manager.Theme);
            Assert.Equal(["Set:Theme", "Save"], manager.CallLog);
        });
    }

    [Fact]
    public void OnNavigatedToAsync_ReadsBackLanguageChangeImmediately_AndSetsCultureOnTranslator() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var translator = new FakeTranslator();
            var vm = CreateViewModel(manager: manager, translator: translator);

            vm.OnNavigatedToAsync();

            var enGb = vm.AvailableLanguages.Single(i => Equals(i.Tag, "en-GB"));
            vm.CurrentLanguage = enGb;

            Assert.Equal("en-GB", manager.Language);
            Assert.Equal(["Set:UILanguage", "Save"], manager.CallLog);
            Assert.Equal(["en-GB"], translator.SetCultureCalls);
        });
    }

    [Fact]
    public void OnNavigatedToAsync_ReadsBackConfirmBeforeExitChangeImmediately_WithExactlyOneSave() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var vm = CreateViewModel(manager: manager);

            vm.OnNavigatedToAsync();

            vm.CurrentConfirmBeforeExit = true;

            Assert.True(manager.ConfirmBeforeExit);
            Assert.Equal(["Set:ConfirmBeforeExit", "Save"], manager.CallLog);
        });
    }

    [Fact]
    public void OnNavigatedToAsync_CalledAloneOrTwice_TriggersNoSave() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var vm = CreateViewModel(manager: manager);

            vm.OnNavigatedToAsync();
            vm.OnNavigatedToAsync();

            Assert.Empty(manager.CallLog);
        });
    }

    [Fact]
    public void SettingSameValueAgain_TriggersNoExtraSave() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var vm = CreateViewModel(manager: manager);

            vm.OnNavigatedToAsync();

            var light = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.Light));
            vm.CurrentTheme = light;

            var callsAfterFirstChange = manager.CallLog.Count;

            // A second ComboBoxItem instance with an equal (but not reference-equal) Tag:
            // proves the no-op guard compares values (Equals), not references (plan D6/B4).
            var anotherLightInstance = new ComboBoxItem { Tag = Theme.Light };
            vm.CurrentTheme = anotherLightInstance;

            Assert.Equal(callsAfterFirstChange, manager.CallLog.Count);
        });
    }

    [Fact]
    public void TwoChangesInQuickSuccession_NeverRunSavesConcurrently_AndFinalStateReflectsTheLastChange() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var firstSaveGate = new TaskCompletionSource();
            var saveStarts = 0;

            manager.OnSaveChangesAsync = _ => {
                var started = Interlocked.Increment(ref saveStarts);

                // Only the first save blocks; this proves a second Set that happens while it
                // is pending does not start its own concurrent SaveChangesAsync (F1).
                return started == 1 ? firstSaveGate.Task : Task.CompletedTask;
            };

            var vm = CreateViewModel(manager: manager);
            vm.OnNavigatedToAsync();

            var light = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.Light));
            var dark = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.Dark));

            vm.CurrentTheme = light;
            Assert.Equal(1, manager.SaveChangesAsyncCallCount);

            vm.CurrentTheme = dark;
            // The second Set already happened; its save must not have started yet.
            Assert.Equal(1, manager.SaveChangesAsyncCallCount);
            Assert.Equal(Theme.Dark, manager.Theme);

            firstSaveGate.SetResult();

            // The chained continuation resumes through the dispatcher's synchronization
            // context (no ConfigureAwait(false) anywhere in the chain, by design); pump the
            // queue until it runs the second save.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (manager.SaveChangesAsyncCallCount < 2 && DateTime.UtcNow < deadline) {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            }

            Assert.Equal(2, manager.SaveChangesAsyncCallCount);
        });
    }

    [Fact]
    public void AppVersion_AfterNavigation_MatchesApplicationContextVersion() {
        StaWindowTestHarness.Run(() => {
            var applicationContext = new FakeApplicationContext { Version = "9.9.9-test" };
            var vm = CreateViewModel(applicationContext: applicationContext);

            vm.OnNavigatedToAsync();

            Assert.Equal("9.9.9-test", vm.AppVersion);
        });
    }

    [Fact]
    public void OpenApplicationLogFileCommand_ExecutesOpenCurrentLogFileRequestThroughMediator() {
        StaWindowTestHarness.Run(() => {
            var mediator = new FakeMediator();
            var vm = CreateViewModel(mediator: mediator);

            vm.OpenApplicationLogFileCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Single(mediator.ExecutedRequests);
            Assert.IsType<OpenCurrentLogFileRequest>(mediator.ExecutedRequests[0]);
        });
    }

    private static AppConfigurationPageViewModel CreateViewModel(
        FakeAppConfigurationManager? manager = null,
        FakeApplicationContext? applicationContext = null,
        FakeTranslator? translator = null,
        FakeMediator? mediator = null) {
        return new AppConfigurationPageViewModel(
            manager ?? new FakeAppConfigurationManager(),
            applicationContext ?? new FakeApplicationContext(),
            translator ?? new FakeTranslator(),
            mediator ?? new FakeMediator(),
            NullLogger<AppConfigurationPageViewModel>.Instance
        );
    }
}

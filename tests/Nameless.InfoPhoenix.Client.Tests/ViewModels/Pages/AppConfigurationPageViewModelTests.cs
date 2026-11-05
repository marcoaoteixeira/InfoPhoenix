using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Features.OpenCurrentLogFile;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.SnackBar;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Tests.ViewModels.Pages;

[Collection(Nameless.InfoPhoenix.Client.Tests.Views.Windows.WpfUiThemeTestCollection.Name)]
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

    /// <summary>
    /// Review round 1, finding 1: <see cref="Theme.System"/> must go through the applier's
    /// system-theme path, not the regular one - <c>ToApplicationTheme()</c> maps it to
    /// <see cref="Nameless.Windows.UI.Theme.System"/>'s WPF-UI equivalent "Unknown", for which
    /// the regular WPF-UI <c>Apply</c> call is a no-op.
    /// </summary>
    [Fact]
    public void OnCurrentThemeChanged_ToSystem_AppliesThroughTheThemeApplier() {
        StaWindowTestHarness.Run(() => {
            // Starts from a non-System theme so changing to System is an actual change (the
            // no-op guard would otherwise swallow it, since System is the manager's default).
            var manager = new FakeAppConfigurationManager { Theme = Theme.Dark };
            var themeApplier = new FakeApplicationThemeApplier();
            var vm = CreateViewModel(manager: manager, themeApplier: themeApplier);

            vm.OnNavigatedToAsync();

            var system = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.System));
            vm.CurrentTheme = system;

            Assert.Equal(Theme.System, themeApplier.AppliedTheme);
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

    /// <summary>
    /// Review round 1, finding 2: an unexpected exception from one save must not permanently
    /// break the save chain - previously, a faulted <c>_pendingSave</c> made every later
    /// <c>SaveAfterAsync</c> rethrow at its unguarded <c>await previous</c> before ever reaching
    /// its own save, silently dropping every save queued after the failure.
    /// </summary>
    [Fact]
    public void UnexpectedSaveFailure_DoesNotPreventALaterSaveFromRunning() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            var saveAttempts = 0;
            manager.OnSaveChangesAsync = _ => {
                saveAttempts++;

                // Not IOException/UnauthorizedAccessException - the kind of failure that used
                // to escape SaveAfterAsync entirely and fault the chain.
                return saveAttempts == 1
                    ? throw new ObjectDisposedException(nameof(FakeAppConfigurationManager))
                    : Task.CompletedTask;
            };

            var vm = CreateViewModel(manager: manager);
            vm.OnNavigatedToAsync();

            vm.CurrentConfirmBeforeExit = true;
            vm.CurrentConfirmBeforeExit = false;

            Assert.Equal(2, manager.SaveChangesAsyncCallCount);
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

    /// <summary>
    /// B1 (plan 2.3 step 2b): an unreadable manager must not throw out of navigation, and
    /// publishes exactly one Failure snackbar.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithAnUnreadableManager_PublishesOneFailureSnackbar_AndDoesNotThrow() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ThrowOnTryGet = new IOException() };
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, eventPublisher: eventPublisher);

            var exception = Record.Exception(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.Null(exception);
            var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(NotificationType.Failure, notification.Type);
        });
    }

    /// <summary>
    /// B2 (plan 2.3, table in 2.3): any single invalid value - wrong type, an undefined enum
    /// value, or a well-formed but unsupported culture - triggers exactly one Failure snackbar.
    /// </summary>
    [Theory]
    [InlineData("Theme", "Dark")] // wrong type relative to the fake's own typed TryGet<T> (it stores a raw string; the real manager serializes Theme as a string, so this case is fake-only)
    [InlineData("Theme", (Theme)42)] // undefined enum value
    [InlineData("UILanguage", 5)] // wrong type (int instead of string)
    [InlineData("UILanguage", "xx-INVALID")] // unresolvable culture tag
    [InlineData("UILanguage", "fr-FR")] // valid culture, not in AvailableLanguages
    public void OnNavigatedToAsync_WithOneInvalidSetting_PublishesExactlyOneFailureSnackbar(string key, object badValue) {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed(key, badValue);
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, eventPublisher: eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(NotificationType.Failure, notification.Type);
        });
    }

    /// <summary>
    /// B3: a wrong-typed <c>ConfirmBeforeExit</c> triggers recovery the same way as the other
    /// two settings.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithWrongTypedConfirmBeforeExit_PublishesExactlyOneFailureSnackbar() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed("ConfirmBeforeExit", "yes");
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, eventPublisher: eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(NotificationType.Failure, notification.Type);
        });
    }

    /// <summary>
    /// B4 (AC2): one bad setting resets ALL three, not only the bad one - exactly three Sets
    /// (in Theme/Language/ConfirmBeforeExit order) followed by exactly one Save, both the
    /// applier and the translator receive the defaults, and neither combo ends up Empty.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithOneBadSettingAmongThree_ResetsAllThreeToDefaults() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed("Theme", (Theme)42); // the only bad one
            manager.Seed("UILanguage", "en-GB"); // valid, non-default
            manager.Seed("ConfirmBeforeExit", true); // valid, non-default
            var themeApplier = new FakeApplicationThemeApplier();
            var translator = new FakeTranslator();
            var vm = CreateViewModel(manager: manager, themeApplier: themeApplier, translator: translator);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            Assert.Equal(["Set:Theme", "Set:UILanguage", "Set:ConfirmBeforeExit", "Save"], manager.CallLog);
            Assert.Equal(Theme.System, manager.Theme);
            Assert.Equal("pt-BR", manager.Language);
            Assert.False(manager.ConfirmBeforeExit);

            // Neither combo is "not found" (ComboBoxItemHelper.EmptyComboBoxItem's Tag is null,
            // AC2's "no stuck/blank state") - both resolve to a real, non-null Tag.
            Assert.Equal(Theme.System, vm.CurrentTheme.Tag);
            Assert.Equal("pt-BR", vm.CurrentLanguage.Tag);

            Assert.Equal(Theme.System, themeApplier.AppliedTheme);
            Assert.Equal(["pt-BR"], translator.SetCultureCalls);
        });
    }

    /// <summary>
    /// B5: the page stays usable after recovery - a further live change still applies and
    /// saves normally.
    /// </summary>
    [Fact]
    public void AfterRecovery_FurtherLiveChanges_StillApplyAndSave() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed("Theme", (Theme)42);
            var vm = CreateViewModel(manager: manager);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();
            var callsAfterRecovery = manager.CallLog.Count;

            var dark = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.Dark));
            vm.CurrentTheme = dark;

            Assert.Equal(Theme.Dark, manager.Theme);
            Assert.Equal(callsAfterRecovery + 2, manager.CallLog.Count); // one Set + one Save
            Assert.Equal("Save", manager.CallLog[^1]);
        });
    }

    /// <summary>
    /// B6 (AC2 "no stuck/blank state"): a persistently unreadable file - every TryGet and every
    /// Set throws - must not crash navigation or a later live change, the VM still shows the
    /// defaults (never Empty), and nothing is ever saved.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WithAPersistentlyUnreadableManager_ShowsDefaults_AndNeverSaves() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager {
                ThrowOnTryGet = new IOException(),
                ThrowOnSet = new IOException()
            };
            var themeApplier = new FakeApplicationThemeApplier();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, themeApplier: themeApplier, eventPublisher: eventPublisher);

            var navigationException = Record.Exception(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.Null(navigationException);
            Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(Theme.System, vm.CurrentTheme.Tag); // not blank (ComboBoxItemHelper.EmptyComboBoxItem's Tag is null)
            Assert.Equal(0, manager.SaveChangesAsyncCallCount);

            var dark = vm.AvailableThemes.Single(i => Equals(i.Tag, Theme.Dark));
            var changeException = Record.Exception(() => vm.CurrentTheme = dark);

            Assert.Null(changeException);
            Assert.Equal(Theme.Dark, themeApplier.AppliedTheme);
            Assert.Equal(0, manager.SaveChangesAsyncCallCount);
        });
    }

    /// <summary>
    /// B14 (plan 2.3 step 5): a failing notification must not break navigation - by the time
    /// the publish is attempted, the Set/Save/applier/translator work has already happened.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WhenPublishingFails_StillCompletes_AfterApplyingAndSavingTheReset() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed("Theme", (Theme)42);
            var themeApplier = new FakeApplicationThemeApplier();
            var translator = new FakeTranslator();
            var eventPublisher = new FakeEventPublisher { ThrowOnPublish = new InvalidOperationException("boom") };
            var vm = CreateViewModel(manager: manager, themeApplier: themeApplier, translator: translator, eventPublisher: eventPublisher);

            var exception = Record.Exception(() => vm.OnNavigatedToAsync().GetAwaiter().GetResult());

            Assert.Null(exception);
            Assert.Equal(["Set:Theme", "Set:UILanguage", "Set:ConfirmBeforeExit", "Save"], manager.CallLog);
            Assert.Equal(Theme.System, themeApplier.AppliedTheme);
            Assert.Equal(["pt-BR"], translator.SetCultureCalls);
        });
    }

    /// <summary>
    /// B7: once a VM instance has recovered, a second navigation is a no-op - no duplicate
    /// publish, Set or Save.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_CalledTwiceOnARecoveredInstance_DoesNotRecoverAgain() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed("Theme", (Theme)42);
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, eventPublisher: eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();
            var callLogAfterFirstNavigation = manager.CallLog.ToList();
            var publishedCountAfterFirstNavigation = eventPublisher.PublishedEvents.Count;

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            Assert.Equal(callLogAfterFirstNavigation, manager.CallLog);
            Assert.Equal(publishedCountAfterFirstNavigation, eventPublisher.PublishedEvents.Count);
        });
    }

    /// <summary>
    /// B8 (AC3, Theory): the happy path never recovers - an empty manager (first run), fully
    /// valid non-default values, and an underscore-normalised culture tag that
    /// <see cref="Nameless.Windows.Helpers.CultureResolver"/> still resolves.
    /// </summary>
    [Theory]
    [InlineData(false, null, null, null)]
    [InlineData(true, "Dark", "en-GB", true)]
    [InlineData(true, "Dark", "pt_BR", true)]
    public void OnNavigatedToAsync_OnTheHappyPath_NeitherRecoversNorPublishes(
        bool seed, string? theme, string? language, bool? confirmBeforeExit) {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            if (seed) {
                manager.Seed("Theme", Enum.Parse<Theme>(theme!));
                manager.Seed("UILanguage", language!);
                manager.Seed("ConfirmBeforeExit", confirmBeforeExit!.Value);
            }
            var themeApplier = new FakeApplicationThemeApplier();
            var translator = new FakeTranslator();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, themeApplier: themeApplier, translator: translator, eventPublisher: eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            Assert.Empty(manager.CallLog);
            Assert.Empty(eventPublisher.PublishedEvents);
            Assert.Empty(themeApplier.AppliedThemes);
            Assert.Empty(translator.SetCultureCalls);
        });
    }

    /// <summary>
    /// B20: the recovery notification's text comes from the translator read AFTER
    /// <c>T.SetCulture(Defaults.Language)</c> has run (plan 2.3 step 5), so it is in the
    /// post-reset language, not whatever language was active before recovery.
    /// </summary>
    [Fact]
    public void OnNavigatedToAsync_WhenRecovering_PublishesTheNotification_AfterSwitchingToTheDefaultLanguage() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Seed("Theme", (Theme)42);
            var translator = new RecordingCultureAtPublishTimeTranslator();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(manager: manager, translator: translator, eventPublisher: eventPublisher);

            vm.OnNavigatedToAsync().GetAwaiter().GetResult();

            Assert.Single(eventPublisher.PublishedEvents);
            Assert.Equal(["pt-BR"], translator.SetCultureCalls);
            Assert.Equal("pt-BR", translator.CultureAtFirstTranslatorRead);
        });
    }

    /// <summary>
    /// Records which culture was active the first time the indexer was read (B20) - a
    /// standalone <see cref="ITranslator"/> fake, since <c>FakeTranslator</c> is sealed and
    /// does not track read order.
    /// </summary>
    private sealed class RecordingCultureAtPublishTimeTranslator : ITranslator {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public List<string> SetCultureCalls { get; } = [];

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

        public string? CultureAtFirstTranslatorRead { get; private set; }

        public string this[string key] {
            get {
                CultureAtFirstTranslatorRead ??= CurrentCulture.Name;

                return key;
            }
        }

        public string this[string key, object[] args] => key;

        public void SetCulture(CultureInfo culture) {
            CurrentCulture = culture;
            SetCultureCalls.Add(culture.Name);
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        }

        public void SetCulture(string isoCode) {
            SetCulture(CultureInfo.GetCultureInfo(isoCode));
        }
    }

    private static AppConfigurationPageViewModel CreateViewModel(
        FakeAppConfigurationManager? manager = null,
        FakeApplicationContext? applicationContext = null,
        FakeApplicationThemeApplier? themeApplier = null,
        ITranslator? translator = null,
        FakeMediator? mediator = null,
        FakeEventPublisher? eventPublisher = null) {
        return new AppConfigurationPageViewModel(
            manager ?? new FakeAppConfigurationManager(),
            applicationContext ?? new FakeApplicationContext(),
            themeApplier ?? new FakeApplicationThemeApplier(),
            translator ?? new FakeTranslator(),
            mediator ?? new FakeMediator(),
            eventPublisher ?? new FakeEventPublisher(),
            NullLogger<AppConfigurationPageViewModel>.Instance
        );
    }
}
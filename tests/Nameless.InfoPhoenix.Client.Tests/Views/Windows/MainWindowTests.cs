using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Dialogs.Message;
using Nameless.Windows.Events;
using Nameless.Windows.Localization;
using Nameless.Windows.Navigation;
using Nameless.Windows.Notification.SnackBar;
using Nameless.Windows.UI;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Windows;

/// <summary>
/// B13/B15 (plan task 7/A1): <c>MainWindow</c> applies the stored theme (not a hard-coded
/// default) at startup, and its <c>Closing</c> handler honours "confirm before exit".
/// </summary>
[Collection(WpfUiThemeTestCollection.Name)]
public class MainWindowTests {
    [Theory]
    [InlineData(Theme.System)]
    [InlineData(Theme.Light)]
    [InlineData(Theme.Dark)]
    [InlineData(Theme.HighContrast)]
    public void Construction_AppliesTheStoredTheme_ForEveryThemeValue(Theme storedTheme) {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager();
            manager.Theme = storedTheme;
            var themeApplier = new FakeApplicationThemeApplier();

            var window = CreateWindow(manager: manager, themeApplier: themeApplier);

            Assert.Equal(storedTheme, themeApplier.AppliedTheme);
            Assert.Same(window, themeApplier.AppliedToWindow);

            window.Close();
        });
    }

    [Fact]
    public void ClosingHandler_WhenConfirmBeforeExitIsFalse_NeverAsksAndNeverCancels() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = false };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Yes };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var args = new CancelEventArgs();
            InvokeClosingHandler(window, args);

            Assert.False(messageDialog.WasShown);
            Assert.False(args.Cancel);

            window.Close();
        });
    }

    [Fact]
    public void ClosingHandler_WhenConfirmBeforeExitIsTrue_AndUserPicksCancel_AbortsClose() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = true };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Cancel };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var args = new CancelEventArgs();
            InvokeClosingHandler(window, args);

            Assert.True(args.Cancel);
            Assert.True(manager.ConfirmBeforeExit);

            // Real Close() re-runs the wired Closing handler; let it succeed this time so the
            // window actually closes and unsubscribes from WPF-UI's static theme watcher.
            messageDialog.ResultToReturn = MessageDialogResult.Yes;
            window.Close();
        });
    }

    /// <summary>
    /// The first pass must cancel this close attempt (not let it through immediately): the fix
    /// for the "No" choice being lost on most exits (review round 1, finding 1) relies on
    /// awaiting the save on the UI thread before re-triggering <see cref="System.Windows.Window.Close"/>,
    /// which is only possible if the current attempt is cancelled first. See
    /// <c>ClosingHandler_WhenConfirmBeforeExitIsTrue_AndUserPicksNo_WaitsForTheSaveBeforeClosingAgain</c>
    /// below for proof the window actually waits.
    /// </summary>
    [Fact]
    public void ClosingHandler_WhenConfirmBeforeExitIsTrue_AndUserPicksNo_CancelsThisAttempt_ThenSavesAndClosesWithoutAskingAgain() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = true };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.No };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var args = new CancelEventArgs();
            InvokeClosingHandler(window, args);

            Assert.True(args.Cancel);
            Assert.False(manager.ConfirmBeforeExit);
            Assert.Equal(1, manager.SaveChangesAsyncCallCount);
            // The re-entrant Close() this triggers must not ask again.
            Assert.Equal(1, messageDialog.ShowCount);
        });
    }

    /// <summary>
    /// Manual QA note (not automatable without a real restart): pick "No" on a build with
    /// ConfirmBeforeExit on, then relaunch the app and confirm it does not ask again - proving
    /// the save actually lands on disk before the process exits, not just in memory.
    /// </summary>
    [Fact]
    public void ClosingHandler_WhenConfirmBeforeExitIsTrue_AndUserPicksNo_WaitsForTheSaveBeforeClosingAgain() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = true };
            var saveGate = new TaskCompletionSource();
            manager.OnSaveChangesAsync = _ => saveGate.Task;
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.No };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var closed = false;
            window.Closed += (_, _) => closed = true;

            var args = new CancelEventArgs();
            InvokeClosingHandler(window, args);

            Assert.True(args.Cancel);
            Assert.False(closed);

            saveGate.SetResult();

            // The continuation resumes through the dispatcher; pump the queue until the
            // re-entrant Close() actually runs.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!closed && DateTime.UtcNow < deadline) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }

            Assert.True(closed);
            Assert.Equal(1, messageDialog.ShowCount);
        });
    }

    /// <summary>
    /// Review round 2 finding: if <c>SaveChangesAsync</c> completes synchronously (e.g. the
    /// real <c>AppConfigurationManager</c> swallowing an I/O failure and returning an
    /// already-completed task), calling <see cref="System.Windows.Window.Close"/> right after
    /// the <c>await</c> throws <see cref="InvalidOperationException"/> because WPF is still
    /// inside this same <c>Closing</c> event - and since the handler is <c>async void</c>, that
    /// exception is unhandled and crashes the app. Exercises the real <see cref="System.Windows.Window.Close"/>
    /// (not reflection) so the second, re-entrant close actually goes through WPF's own
    /// re-entrancy guard, and pumps the dispatcher so the deferred <c>Close()</c> gets a chance
    /// to run.
    /// </summary>
    [Fact]
    public void ClosingHandler_WhenSaveCompletesSynchronously_ClosesWithoutThrowing() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = true };
            manager.OnSaveChangesAsync = _ => Task.CompletedTask;
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.No };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var closed = false;
            window.Closed += (_, _) => closed = true;

            var exception = Record.Exception(window.Close);
            Assert.Null(exception);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!closed && DateTime.UtcNow < deadline) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }

            Assert.True(closed);
            Assert.Equal(1, messageDialog.ShowCount);
        });
    }

    [Fact]
    public void ClosingHandler_WhenConfirmBeforeExitIsTrue_AndUserPicksYes_ClosesWithoutChangingTheSetting() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = true };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Yes };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var args = new CancelEventArgs();
            InvokeClosingHandler(window, args);

            Assert.False(args.Cancel);
            Assert.True(manager.ConfirmBeforeExit);

            window.Close();
        });
    }

    /// <summary>
    /// B10 (plan 2.5): the latent IPX-0004 bug fix - without this subscription, no snackbar
    /// published through <see cref="IEventPublisher"/> is ever shown by the running app.
    /// </summary>
    [Fact]
    public void Construction_SubscribesExactlyOnceToSnackBarNotificationEvent() {
        StaWindowTestHarness.Run(() => {
            var eventPublisher = new FakeEventPublisher();

            var window = CreateWindow(eventPublisher: eventPublisher);

            Assert.Single(eventPublisher.Registrations, r => r.EventType == typeof(SnackBarNotificationEvent));

            window.Close();
        });
    }

    /// <summary>
    /// B11 (plan 2.2): an unreadable manager must not crash construction - the startup theme
    /// read falls back to the default (System), which <see cref="InitializeWindow"/> still
    /// applies through the theme applier.
    /// </summary>
    [Fact]
    public void Construction_WithAThrowingManager_DoesNotThrow_AndAppliesTheDefaultTheme() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ThrowOnTryGet = new IOException() };
            var themeApplier = new FakeApplicationThemeApplier();

            Nameless.InfoPhoenix.Client.Views.Windows.MainWindow? window = null;
            var exception = Record.Exception(() => window = CreateWindow(manager: manager, themeApplier: themeApplier));

            Assert.Null(exception);
            Assert.Equal(Constants.AppConfiguration.Defaults.Theme, themeApplier.AppliedTheme);

            window!.Close();
        });
    }

    /// <summary>
    /// B12 (plan 2.2): a throwing <c>ConfirmBeforeExit</c> read must not block or crash the
    /// close - it is treated as "do not ask", so the close goes through without a dialog.
    /// </summary>
    [Fact]
    public void ClosingHandler_WithAThrowingConfirmBeforeExitRead_ClosesWithoutAskingOrThrowing() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ThrowOnTryGet = new IOException() };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Yes };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var args = new CancelEventArgs();
            var exception = Record.Exception(() => InvokeClosingHandler(window, args));

            Assert.Null(exception);
            Assert.False(args.Cancel);
            Assert.False(messageDialog.WasShown);

            window.Close();
        });
    }

    private static void InvokeClosingHandler(Nameless.InfoPhoenix.Client.Views.Windows.MainWindow window, CancelEventArgs args) {
        var method = typeof(Nameless.InfoPhoenix.Client.Views.Windows.MainWindow).GetMethod(
            "ClosingHandler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;

        method.Invoke(window, [window, args]);
    }

    private static Nameless.InfoPhoenix.Client.Views.Windows.MainWindow CreateWindow(
        FakeAppConfigurationManager? manager = null,
        FakeMessageDialog? messageDialog = null,
        FakeApplicationThemeApplier? themeApplier = null,
        FakeEventPublisher? eventPublisher = null) {
        var viewModel = new MainWindowViewModel(new FakeApplicationContext(), new NoOpNavigationViewItemProvider());

        return new Nameless.InfoPhoenix.Client.Views.Windows.MainWindow(
            viewModel,
            manager ?? new FakeAppConfigurationManager(),
            themeApplier ?? new FakeApplicationThemeApplier(),
            new NoOpContentDialogService(),
            eventPublisher ?? new FakeEventPublisher(),
            NullLogger<Nameless.InfoPhoenix.Client.Views.Windows.MainWindow>.Instance,
            new FakeTranslator(),
            messageDialog ?? new FakeMessageDialog(),
            new NoOpNavigationService(),
            new NoOpNavigationViewPageProvider(),
            new NoOpSnackbarService()
        );
    }

    private sealed class NoOpNavigationViewItemProvider : INavigationViewItemProvider {
        public IEnumerable<NavigationViewItem> GetItems(Placement placement) {
            return [];
        }
    }

    private sealed class NoOpContentDialogService : IContentDialogService {
        public void SetContentPresenter(System.Windows.Controls.ContentPresenter contentPresenter) { }

        public System.Windows.Controls.ContentPresenter GetContentPresenter() {
            throw new NotSupportedException();
        }

        public void SetDialogHost(System.Windows.Controls.ContentPresenter contentPresenter) { }

        public void SetDialogHost(ContentDialogHost dialogHost) { }

        public System.Windows.Controls.ContentPresenter GetDialogHost() {
            throw new NotSupportedException();
        }

        public ContentDialogHost GetDialogHostEx() {
            throw new NotSupportedException();
        }

        public Task<ContentDialogResult> ShowAsync(ContentDialog dialog, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }
    }

    private sealed class NoOpNavigationService : INavigationService {
        public bool Navigate(Type pageType) {
            return true;
        }

        public bool Navigate(Type pageType, object? dataContext) {
            return true;
        }

        public bool Navigate(string pageIdOrName) {
            return true;
        }

        public bool Navigate(string pageIdOrName, object? dataContext) {
            return true;
        }

        public bool NavigateWithHierarchy(Type pageType) {
            return true;
        }

        public bool NavigateWithHierarchy(Type pageType, object? dataContext) {
            return true;
        }

        public INavigationView GetNavigationControl() {
            throw new NotSupportedException();
        }

        public void SetNavigationControl(INavigationView navigationControl) { }

        public bool GoBack() {
            return true;
        }
    }

    private sealed class NoOpNavigationViewPageProvider : Wpf.Ui.Abstractions.INavigationViewPageProvider {
        public object GetPage(Type pageType) {
            throw new NotSupportedException();
        }
    }

    private sealed class NoOpSnackbarService : ISnackbarService {
        public TimeSpan DefaultTimeOut { get; set; }

        public void SetSnackbarPresenter(SnackbarPresenter contentPresenter) { }

        public SnackbarPresenter GetSnackbarPresenter() {
            throw new NotSupportedException();
        }

        public void Show(string title, string message, ControlAppearance appearance, IconElement? icon, TimeSpan timeout) { }
    }

    private sealed class FakeMessageDialog : IMessageDialog {
        public MessageDialogResult ResultToReturn { get; set; } = MessageDialogResult.Ok;

        public bool WasShown => ShowCount > 0;

        public int ShowCount { get; private set; }

        public MessageDialogResult Show(string message, Action<MessageDialogOptions> configure) {
            ShowCount++;

            return ResultToReturn;
        }
    }
}

using System.ComponentModel;
using System.Windows.Controls;
using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Dialogs.Message;
using Nameless.Windows.Localization;
using Nameless.Windows.Navigation;
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

    [Fact]
    public void ClosingHandler_WhenConfirmBeforeExitIsTrue_AndUserPicksNo_StopsAskingAndCloses() {
        StaWindowTestHarness.Run(() => {
            var manager = new FakeAppConfigurationManager { ConfirmBeforeExit = true };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.No };
            var window = CreateWindow(manager: manager, messageDialog: messageDialog);

            var args = new CancelEventArgs();
            InvokeClosingHandler(window, args);

            Assert.False(args.Cancel);
            Assert.False(manager.ConfirmBeforeExit);

            window.Close();
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
        FakeApplicationThemeApplier? themeApplier = null) {
        var viewModel = new MainWindowViewModel(new FakeApplicationContext(), new NoOpNavigationViewItemProvider());

        return new Nameless.InfoPhoenix.Client.Views.Windows.MainWindow(
            viewModel,
            manager ?? new FakeAppConfigurationManager(),
            themeApplier ?? new FakeApplicationThemeApplier(),
            new NoOpContentDialogService(),
            new FakeTranslator(),
            messageDialog ?? new FakeMessageDialog(),
            new NoOpNavigationService(),
            new NoOpNavigationViewPageProvider(),
            new NoOpSnackbarService()
        );
    }

    private sealed class FakeApplicationThemeApplier : IApplicationThemeApplier {
        public System.Windows.Window? AppliedToWindow { get; private set; }

        public Nameless.Windows.UI.Theme? AppliedTheme { get; private set; }

        public void Apply(System.Windows.Window window, Nameless.Windows.UI.Theme theme) {
            AppliedToWindow = window;
            AppliedTheme = theme;
        }
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

        public bool WasShown { get; private set; }

        public MessageDialogResult Show(string message, Action<MessageDialogOptions> configure) {
            WasShown = true;

            return ResultToReturn;
        }
    }
}

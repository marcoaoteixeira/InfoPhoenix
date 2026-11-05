using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Dialogs.Message;
using Nameless.Windows.Events;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification.SnackBar;
using Nameless.Windows.UI;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class MainWindow : INavigationWindow {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IApplicationThemeApplier _applicationThemeApplier;
    private readonly IContentDialogService _contentDialogService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<MainWindow> _logger;
    private readonly IMessageDialog _messageDialog;
    private readonly INavigationService _navigationService;
    private readonly INavigationViewPageProvider _navigationViewPageProvider;
    private readonly ISnackbarService _snackBarService;

    private bool _initialized;

    // Guards the re-entrant Close() call below: without it, the second pass through
    // ClosingHandler would ask the "stop asking" question again.
    private bool _closeConfirmed;

    private ITranslator T { get; }

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        IAppConfigurationManager appConfigurationManager,
        IApplicationThemeApplier applicationThemeApplier,
        IContentDialogService contentDialogService,
        IEventPublisher eventPublisher,
        ILogger<MainWindow> logger,
        ITranslator translator,
        IMessageDialog messageDialog,
        INavigationService navigationService,
        INavigationViewPageProvider navigationViewPageProvider,
        ISnackbarService snackBarService) {
        _appConfigurationManager = appConfigurationManager;
        _applicationThemeApplier = applicationThemeApplier;
        _contentDialogService = contentDialogService;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _messageDialog = messageDialog;
        _navigationService = navigationService;
        _navigationViewPageProvider = navigationViewPageProvider;
        _snackBarService = snackBarService;

        ViewModel = viewModel;
        DataContext = ViewModel;
        T = translator;

        InitializeComponent();
        InitializeWindow();
    }

    public INavigationView GetNavigation() {
        return NavigationViewRoot;
    }

    public bool Navigate(Type pageType) {
        return GetNavigation().Navigate(pageType);
    }

    public void SetServiceProvider(IServiceProvider serviceProvider) {
        throw new NotImplementedException();
    }

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) {
        GetNavigation().SetPageProviderService(navigationViewPageProvider);
    }

    public void ShowWindow() {
        Show();
    }

    public void CloseWindow() {
        Close();
    }

    /// <summary>
    /// Honours the "confirm before exit" setting on every close attempt: Cancel aborts the
    /// close, No also stops asking (clears the setting) and closes, Yes closes without
    /// changing the setting (plan task 7/A1, ported from the sample).
    /// </summary>
    /// <remarks>
    /// On No, the "stop asking" change must be on disk before the process actually exits -
    /// relying on <c>App.OnCurrentHostStop</c> is not enough, since <c>OnExit</c> is
    /// <c>async void</c> and the process can end as soon as it hits its first incomplete
    /// await, before that save's write lands. So this cancels the current close attempt,
    /// awaits the save (the dispatcher is still alive here, unlike inside <c>OnExit</c>), then
    /// re-triggers <see cref="Close"/> now that it is safely persisted. Never blocks with
    /// <c>GetAwaiter().GetResult()</c> instead: that would deadlock the UI thread if the save's
    /// continuation needs the same thread to resume.
    /// </remarks>
    private async void ClosingHandler(object? sender, CancelEventArgs args) {
        if (_closeConfirmed || !TryReadConfirmBeforeExit()) { return; }

        var result = _messageDialog.ShowQuestion(
            title: T["MainWindow_ConfirmApplicationExit_MessageBox_Title"],
            message: T["MainWindow_ConfirmApplicationExit_MessageBox_Message"],
            buttons: MessageDialogButtons.YesNoCancel
        );

        if (result == MessageDialogResult.Cancel) {
            args.Cancel = true;
            return;
        }

        if (result != MessageDialogResult.No) {
            // Yes: close without changing the setting.
            return;
        }

        args.Cancel = true;

        try {
            _appConfigurationManager.ConfirmBeforeExit = false;

            await _appConfigurationManager.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception) {
            // Unexpected failure (including a Set that throws on a persistently unreadable
            // file, plan 2.2): still honour the user's choice to close rather than leaving the
            // app stuck with the close silently swallowed.
        }

        _closeConfirmed = true;

        // Queue the re-entrant Close() instead of calling it inline: if the save above
        // completed synchronously, WPF is still inside this Closing event, and calling
        // Close() again right now throws InvalidOperationException ("a Window is closing").
        // Dispatching it defers the call until this Closing event has fully unwound.
        _ = Dispatcher.BeginInvoke(Close);
    }

    private void InitializeWindow() {
        if (_initialized) { return; }

        _applicationThemeApplier.Apply(this, TryReadTheme());

        _contentDialogService.SetDialogHost(ContentDialogHostRoot);
        _navigationService.SetNavigationControl(NavigationViewRoot);
        SetPageService(_navigationViewPageProvider);
        _snackBarService.SetSnackbarPresenter(SnackBarPresenterRoot);

        SubscribeForNotifications();

        _initialized = true;
    }

    /// <summary>
    /// Ports the sample's subscription (plan 2.5) - without it, no snackbar published through
    /// <see cref="IEventPublisher"/> is ever shown, including this ticket's recovery
    /// notification and IPX-0004's "missing log file" warning (a latent bug, required for AC1).
    /// <c>MainWindow</c> is resolved once, before the splash, and lives for the whole process,
    /// so no <c>Unregister</c> is needed.
    /// </summary>
    private void SubscribeForNotifications() {
        _eventPublisher.Register<SnackBarNotificationEvent>(this, ShowNotificationInSnackBar);
    }

    private void ShowNotificationInSnackBar(object sender, SnackBarNotificationEvent evt) {
        Dispatcher.InvokeAsync(() => _snackBarService.Show(evt));
    }

    /// <summary>
    /// Guards the startup theme read (plan 2.2): an unreadable <c>app.config</c> must not crash
    /// the app before the page can ever recover anything (plan risk "or the app").
    /// </summary>
    private Theme TryReadTheme() {
        try {
            return _appConfigurationManager.Theme;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to read the stored theme; falling back to the default.");

            return Constants.AppConfiguration.Defaults.Theme;
        }
    }

    /// <summary>
    /// Guards the closing-time read (plan 2.2): on an unreadable manager, treat it as "do not
    /// ask" so the window still closes rather than crashing the <c>async void</c> handler.
    /// </summary>
    private bool TryReadConfirmBeforeExit() {
        try {
            return _appConfigurationManager.ConfirmBeforeExit;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to read the stored 'confirm before exit' setting; closing without asking.");

            return false;
        }
    }
}
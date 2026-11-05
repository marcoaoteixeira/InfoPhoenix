using System.ComponentModel;
using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Dialogs.Message;
using Nameless.Windows.Localization;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class MainWindow : INavigationWindow {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IApplicationThemeApplier _applicationThemeApplier;
    private readonly IContentDialogService _contentDialogService;
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
        ITranslator translator,
        IMessageDialog messageDialog,
        INavigationService navigationService,
        INavigationViewPageProvider navigationViewPageProvider,
        ISnackbarService snackBarService) {
        _appConfigurationManager = appConfigurationManager;
        _applicationThemeApplier = applicationThemeApplier;
        _contentDialogService = contentDialogService;
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
        if (_closeConfirmed || !_appConfigurationManager.ConfirmBeforeExit) { return; }

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
        _appConfigurationManager.ConfirmBeforeExit = false;

        await _appConfigurationManager.SaveChangesAsync(CancellationToken.None);

        _closeConfirmed = true;
        Close();
    }

    private void InitializeWindow() {
        if (_initialized) { return; }

        _applicationThemeApplier.Apply(this, _appConfigurationManager.Theme);

        _contentDialogService.SetDialogHost(ContentDialogHostRoot);
        _navigationService.SetNavigationControl(NavigationViewRoot);
        SetPageService(_navigationViewPageProvider);
        _snackBarService.SetSnackbarPresenter(SnackBarPresenterRoot);

        _initialized = true;
    }
}

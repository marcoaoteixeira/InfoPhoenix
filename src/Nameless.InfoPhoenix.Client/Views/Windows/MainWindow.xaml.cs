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
    private void ClosingHandler(object? sender, CancelEventArgs args) {
        if (!_appConfigurationManager.ConfirmBeforeExit) { return; }

        var result = _messageDialog.ShowQuestion(
            title: T["MainWindow_ConfirmApplicationExit_MessageBox_Title"],
            message: T["MainWindow_ConfirmApplicationExit_MessageBox_Message"],
            buttons: MessageDialogButtons.YesNoCancel
        );

        if (result == MessageDialogResult.No) {
            _appConfigurationManager.ConfirmBeforeExit = false;
        }

        args.Cancel = result == MessageDialogResult.Cancel;
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

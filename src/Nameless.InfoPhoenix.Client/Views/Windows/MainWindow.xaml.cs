using System.ComponentModel;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.Notifications;
using Nameless.InfoPhoenix.Client.Resources;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;
using Nameless.WPF;
using Nameless.WPF.Configuration;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Dialogs.Message;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Notifications;
using Nameless.WPF.Snackbar;
using Nameless.WPF.UseCases.SystemUpdate.Check;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

[ServiceLifetime(Lifetime = ServiceLifetime.Singleton)]
public partial class MainWindow : INavigationWindow, IHasViewModel<MainWindowViewModel> {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IContentDialogService _contentDialogService;
    private readonly IMessageDialog _messageDialog;
    private readonly INavigationService _navigationService;
    private readonly INavigationViewPageProvider _navigationViewPageProvider;
    private readonly INotificationService _notificationService;
    private readonly ISnackbarService _snackBarService;
    private readonly ILogger<MainWindow> _logger;

    private bool _initialized;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        IAppConfigurationManager appConfigurationManager,
        IContentDialogService contentDialogService,
        IMessageDialog messageDialog,
        INavigationService navigationService,
        INavigationViewPageProvider navigationViewPageProvider,
        INotificationService notificationService,
        ISnackbarService snackBarService,
        ILogger<MainWindow> logger) {

        ViewModel = viewModel;

        DataContext = ViewModel;

        _appConfigurationManager = appConfigurationManager;
        _contentDialogService = contentDialogService;
        _messageDialog = messageDialog;
        _navigationService = navigationService;
        _navigationViewPageProvider = navigationViewPageProvider;
        _notificationService = notificationService;
        _snackBarService = snackBarService;
        _logger = logger;

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

    private void ClosingHandler(object? _, CancelEventArgs args) {
        if (!_appConfigurationManager.GetConfirmBeforeExit()) {
            return;
        }

        var result = _messageDialog.ShowQuestion(
            title: Strings.MainWindow_ConfirmApplicationExit_MessageBox_Title,
            message: Strings.MainWindow_ConfirmApplicationExit_MessageBox_Message,
            buttons: MessageDialogButtons.YesNoCancel);

        if (result == MessageDialogResult.No) {
            _appConfigurationManager.SetConfirmBeforeExit(false);
        }

        args.Cancel = result == MessageDialogResult.Cancel;
    }

    private void InitializeWindow() {
        if (_initialized) { return; }

        SetApplicationTheme();
        SetContentDialogHostRoot();
        SetNavigationView();
        SetPageService(_navigationViewPageProvider);
        SetSnackBarPresenter();
        SetWindowIcon();
        SubscribeForNotifications();

        _initialized = true;
    }

    private void SetApplicationTheme() {
        SystemThemeWatcher.Watch(this);

        var currentTheme = _appConfigurationManager.GetTheme()
                                                   .ToApplicationTheme();

        ApplicationThemeManager.Apply(currentTheme);
    }

    private void SetContentDialogHostRoot() {
        _contentDialogService.SetDialogHost(ContentDialogHostRoot);
    }

    private void SetNavigationView() {
        _navigationService.SetNavigationControl(NavigationViewRoot);
    }

    private void SetSnackBarPresenter() {
        _snackBarService.SetSnackbarPresenter(SnackbarPresenterRoot);
    }

    private void SetWindowIcon() {
        try { Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/application_64x64.png")); }
        catch (Exception ex) { _logger.SetWindowIconFailure(ex); }
    }

    private void SubscribeForNotifications() {
        _notificationService.Subscribe<CheckForUpdateNotification>(this, ShowSnackBarNotification);
        _notificationService.Subscribe<CreateNewDocumentDirectoryNotification>(this, ShowSnackBarNotification);
        _notificationService.Subscribe<UpdateDocumentDirectoryNotification>(this, ShowSnackBarNotification);
        _notificationService.Subscribe<DocumentDirectoryManagementNotification>(this, ShowSnackBarNotification);

    }

    private void ShowSnackBarNotification(object sender, INotification notification) {
        Dispatcher.Invoke(() => _snackBarService.Show(notification.ToSnackbarParameters()));
    }
}
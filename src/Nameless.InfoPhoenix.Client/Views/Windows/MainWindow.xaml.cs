using System.ComponentModel;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Application.ErrorHandling;
using Nameless.InfoPhoenix.Application.Notification;
using Nameless.InfoPhoenix.Client.Notifications;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Dialogs;
using Nameless.InfoPhoenix.Domains.UseCases.Database.PerformBackup;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Indexing;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.UpdateContent;
using Nameless.InfoPhoenix.Domains.UseCases.Search;
using Nameless.InfoPhoenix.Notification;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class MainWindow : INavigationWindow, INotificationAware {
    private readonly IAppConfigurationManager _appConfigurationContext;
    private readonly IContentDialogService _contentDialogService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IPageService _pageService;
    private readonly ISnackbarService _snackbarService;
    private readonly ILogger<MainWindow> _logger;

    private bool _initialized;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel,
                      IAppConfigurationManager appConfigurationContext,
                      IContentDialogService contentDialogService,
                      IDialogService dialogService,
                      INavigationService navigationService,
                      INotificationService notificationService,
                      IPageService pageService,
                      ISnackbarService snackbarService,
                      ILogger<MainWindow> logger) {
        ViewModel = Prevent.Argument.Null(viewModel);

        DataContext = ViewModel;

        _appConfigurationContext = Prevent.Argument.Null(appConfigurationContext);
        _contentDialogService = Prevent.Argument.Null(contentDialogService);
        _dialogService = Prevent.Argument.Null(dialogService);
        _navigationService = Prevent.Argument.Null(navigationService);
        _notificationService = Prevent.Argument.Null(notificationService);
        _pageService = Prevent.Argument.Null(pageService);
        _snackbarService = Prevent.Argument.Null(snackbarService);
        _logger = Prevent.Argument.Null(logger);

        InitializeComponent();
        Initialize();
    }

    public INavigationView GetNavigation()
        => NavigationViewRoot;

    public bool Navigate(Type pageType)
        => GetNavigation().Navigate(pageType);

    public void SetServiceProvider(IServiceProvider serviceProvider) { }

    public void SetPageService(IPageService pageService)
        => GetNavigation().SetPageService(pageService);

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();

    public void SubscribeForNotifications() {
        _notificationService.Subscribe<ExceptionWardenNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));

        _notificationService.Subscribe<DocumentDirectorySavedNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
        _notificationService.Subscribe<DatabaseBackupPerformedNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
        _notificationService.Subscribe<DocumentDirectoryPathCopiedToClipboardNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
        _notificationService.Subscribe<DocumentDirectoryMissingNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
        _notificationService.Subscribe<DocumentDirectoryIndexedNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
        _notificationService.Subscribe<UpdateDocumentDirectoryContentCompletedNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));

        _notificationService.Subscribe<SearchDocumentErrorNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
    }

    public void UnsubscribeFromNotifications() {
        _notificationService.Unsubscribe<ExceptionWardenNotification>(this);

        _notificationService.Unsubscribe<DocumentDirectorySavedNotification>(this);
        _notificationService.Unsubscribe<DatabaseBackupPerformedNotification>(this);
        _notificationService.Unsubscribe<DocumentDirectoryPathCopiedToClipboardNotification>(this);
        _notificationService.Unsubscribe<DocumentDirectoryMissingNotification>(this);
        _notificationService.Unsubscribe<DocumentDirectoryIndexedNotification>(this);
        _notificationService.Unsubscribe<UpdateDocumentDirectoryContentCompletedNotification>(this);

        _notificationService.Unsubscribe<SearchDocumentErrorNotification>(this);
    }

    protected override void OnContentRendered(EventArgs args) {
        base.OnContentRendered(args);

        Navigate(typeof(SearchPage));
    }

    private void Initialize() {
        if (_initialized) { return; }
        
        SetApplicationTheme();
        SetContentDialogPresenter();
        SetMainWindowIcon();
        SetNavigationPresenter();
        SetPageService(_pageService);
        SetSnackbarPresenter();
        SubscribeForNotifications();

        _initialized = true;
    }

    private void SetApplicationTheme() {
        SystemThemeWatcher.Watch(this);
        var currentTheme = _appConfigurationContext.GetTheme();
        ApplicationThemeManager.Apply(currentTheme.ToApplicationTheme());
    }

    private void SetContentDialogPresenter()
        => _contentDialogService.SetDialogHost(ContentDialogRoot);

    private void SetMainWindowIcon() {
        try { Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/branding/info_phoenix_64x64.png")); }
        catch (Exception ex) { _logger.LogError(ex, "Could not load resource info_phoenix_64x64"); }
    }

    private void SetNavigationPresenter()
        => _navigationService.SetNavigationControl(NavigationViewRoot);

    private void SetSnackbarPresenter()
        => _snackbarService.SetSnackbarPresenter(SnackbarPresenterRoot);
    
    private void SnackbarNotificationHandler(NotificationBase notification)
        => Dispatcher.InvokeAsync(() => _snackbarService.Show(notification.ToSnackbarParameters()));

    private void ClosingHandler(object? _, CancelEventArgs args) {
        if (!_appConfigurationContext.GetConfirmBeforeExit()) {
            return;
        }

        var result = _dialogService.ShowQuestion(title: "Sair",
                                                 message: "Sempre perguntar ao sair do aplicativo?",
                                                 buttons: DialogButtons.YesNoCancel);

        if (result == Dialogs.DialogResult.No) {
            _appConfigurationContext.SetConfirmBeforeExit(false);
        }

        args.Cancel = result == Dialogs.DialogResult.Cancel;
    }
}
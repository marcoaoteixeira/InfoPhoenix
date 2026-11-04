using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class MainWindow : INavigationWindow {
    private readonly IContentDialogService _contentDialogService;
    private readonly INavigationService _navigationService;
    private readonly INavigationViewPageProvider _navigationViewPageProvider;
    private readonly ISnackbarService _snackBarService;

    private bool _initialized;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        IContentDialogService contentDialogService,
        INavigationService navigationService,
        INavigationViewPageProvider navigationViewPageProvider,
        ISnackbarService snackBarService) {
        _contentDialogService = contentDialogService;
        _navigationService = navigationService;
        _navigationViewPageProvider = navigationViewPageProvider;
        _snackBarService = snackBarService;

        ViewModel = viewModel;
        DataContext = ViewModel;

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

    private void InitializeWindow() {
        if (_initialized) { return; }

        SystemThemeWatcher.Watch(this);
        ApplicationThemeManager.Apply(ApplicationTheme.Light);

        _contentDialogService.SetDialogHost(ContentDialogHostRoot);
        _navigationService.SetNavigationControl(NavigationViewRoot);
        SetPageService(_navigationViewPageProvider);
        _snackBarService.SetSnackbarPresenter(SnackBarPresenterRoot);

        _initialized = true;
    }
}

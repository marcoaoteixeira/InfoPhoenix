using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.Application;
using Nameless.InfoPhoenix.Client.Infrastructure.Events;
using Nameless.Windows.Events;
using Nameless.Windows.Mvvm;
using Nameless.Windows.Navigation;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModel {
    private bool _initialized;

    private readonly IApplicationContext _applicationContext;
    private readonly INavigationViewItemProvider _navigationViewItemProvider;
    private readonly IEventPublisher _eventPublisher;

    private readonly Lazy<string> _appTitle;
    private readonly Lazy<string> _appVersion;

    public string AppTitle => _appTitle.Value;
    public string AppVersion => _appVersion.Value;

    [ObservableProperty]
    private NavigationViewItem[] _mainMenuItemsSource = [];

    [ObservableProperty]
    private NavigationViewItem[] _footerMenuItemsSource = [];

    public MainWindowViewModel(IApplicationContext applicationContext, INavigationViewItemProvider navigationViewItemProvider, IEventPublisher eventPublisher) {
        _applicationContext = applicationContext;
        _navigationViewItemProvider = navigationViewItemProvider;
        _eventPublisher = eventPublisher;

        _appTitle = new Lazy<string>(GetAppTitle);
        _appVersion = new Lazy<string>(GetAppVersion);

        Initialize();
    }

    private void Initialize() {
        if (_initialized) { return; }

        UpdateMenu();
        SubscribeForEvents();

        _initialized = true;
    }

    private string GetAppTitle() {
        return _applicationContext.ApplicationName;
    }

    private string GetAppVersion() {
        return _applicationContext.Version;
    }

    [RelayCommand]
    public Task UpdateMenuAsync() {
        UpdateMenu();

        return Task.CompletedTask;
    }

    private void UpdateMenu() {
        MainMenuItemsSource = [.. _navigationViewItemProvider.GetMenuItems()];
        FooterMenuItemsSource = [.. _navigationViewItemProvider.GetFooterItems()];
    }

    private void SubscribeForEvents() {
        _eventPublisher.Register<CultureChangeEvent>(this, CultureChangeEventHandler);
    }

    private void CultureChangeEventHandler(object sender, CultureChangeEvent evt) {
        UpdateMenu();
    }
}

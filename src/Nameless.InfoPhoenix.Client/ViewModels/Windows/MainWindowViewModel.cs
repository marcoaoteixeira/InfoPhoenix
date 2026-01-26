using Nameless.Infrastructure;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Navigation;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public class MainWindowViewModel : ViewModel {
    private readonly IApplicationContext _applicationContext;
    private readonly INavigationViewItemProvider _navigationViewItemProvider;

    private readonly Lazy<string> _appTitle;
    private readonly Lazy<string> _appVersion;
    private readonly Lazy<NavigationViewItem[]> _mainMenuItemsSource;
    private readonly Lazy<NavigationViewItem[]> _footerMenuItemsSource;

    public string AppTitle => _appTitle.Value;
    public string AppVersion => _appVersion.Value;
    public NavigationViewItem[] MainMenuItemsSource => _mainMenuItemsSource.Value;
    public NavigationViewItem[] FooterMenuItemsSource => _footerMenuItemsSource.Value;

    public MainWindowViewModel(IApplicationContext applicationContext, INavigationViewItemProvider navigationViewItemProvider) {
        _applicationContext = applicationContext;
        _navigationViewItemProvider = navigationViewItemProvider;

        _appTitle = new Lazy<string>(GetAppTitle);
        _appVersion = new Lazy<string>(GetAppVersion);
        _mainMenuItemsSource = new Lazy<NavigationViewItem[]>(GetMainMenuItemsSource);
        _footerMenuItemsSource = new Lazy<NavigationViewItem[]>(GetFooterMenuItemsSource);
    }

    private NavigationViewItem[] GetMainMenuItemsSource() {
        return _navigationViewItemProvider.GetMainItems().ToArray();
    }

    private NavigationViewItem[] GetFooterMenuItemsSource() {
        return _navigationViewItemProvider.GetFooterItems().ToArray();
    }

    private string GetAppTitle() {
        return _applicationContext.ApplicationName;
    }

    private string GetAppVersion() {
        return _applicationContext.Version;
    }
}
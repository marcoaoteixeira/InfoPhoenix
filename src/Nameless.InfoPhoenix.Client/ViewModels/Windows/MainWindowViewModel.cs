using System.Windows;
using Nameless.Application;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public sealed class MainWindowViewModel {
    private readonly IApplicationContext _applicationContext;

    private bool _initialized;

    public string AppTitle { get; private set; } = string.Empty;
    public string AppVersion { get; private set; } = string.Empty;
    public NavigationViewItem[] MenuItemsSource { get; private set; } = [];
    public NavigationViewItem[] FooterMenuItemsSource { get; private set; } = [];

    public MainWindowViewModel(IApplicationContext applicationContext) {
        _applicationContext = Prevent.Argument.Null(applicationContext);

        Initialize();
    }

    private void Initialize() {
        if (_initialized) { return; }

        AppTitle = _applicationContext.AppName;
        AppVersion = _applicationContext.Version;

        MenuItemsSource = [
            new NavigationViewItem {
                Content = SearchPage.PageName,
                Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentSearch24 },
                TargetPageType = typeof(SearchPage),
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = SearchPage.PageToolTip,
                FontSize = 20
            },
            new NavigationViewItem {
                Content = DocumentDirectoriesPage.PageName,
                Icon = new SymbolIcon { Symbol = SymbolRegular.Folder48 },
                TargetPageType = typeof(DocumentDirectoriesPage),
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = DocumentDirectoriesPage.PageToolTip,
                FontSize = 20
            }
        ];

        FooterMenuItemsSource = [
            new NavigationViewItem {
                Content = AppConfigurationPage.PageName,
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings48 },
                TargetPageType = typeof(AppConfigurationPage),
                ToolTip = AppConfigurationPage.PageToolTip
            }
        ];

        _initialized = true;
    }
}
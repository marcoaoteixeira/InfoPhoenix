using System.Windows;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.Infrastructure;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows {
    public sealed class MainWindowViewModel {
        #region Private Read-Only Fields

        private readonly IApplicationContext _applicationContext;

        #endregion

        #region Private Properties

        private bool Initialized { get; set; }

        #endregion

        #region Public Properties

        public string AppTitle { get; private set; } = string.Empty;
        public string AppVersion { get; private set; } = string.Empty;
        public NavigationViewItem[] MenuItemsSource { get; private set; } = [];
        public NavigationViewItem[] FooterMenuItemsSource { get; private set; } = [];

        #endregion

        #region Public Constructors

        public MainWindowViewModel(IApplicationContext applicationContext) {
            _applicationContext = Guard.Against.Null(applicationContext, nameof(applicationContext));

            Initialize();
        }

        #endregion

        #region Private Methods

        private void Initialize() {
            if (Initialized) { return; }

            AppTitle = _applicationContext.ApplicationName;
            AppVersion = _applicationContext.SemVer;

            MenuItemsSource = [
                new NavigationViewItem {
                    Content = "Pesquisar",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentSearch24 },
                    TargetPageType = typeof(SearchPage),
                    Margin = new Thickness(0, 0, 0, 10),
                    ToolTip = "Pesquisar",
                    FontSize = 20
                },
                new NavigationViewItem {
                    Content = "Diretórios",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Folder48 },
                    TargetPageType = typeof(ListDocumentDirectoryPage),
                    Margin = new Thickness(0, 0, 0, 10),
                    ToolTip = "Diretórios",
                    FontSize = 20
                }
            ];

            FooterMenuItemsSource = [
                new NavigationViewItem {
                    Content = "Configurações",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings48 },
                    TargetPageType = typeof(AppConfigurationPage),
                    ToolTip = "Configurações"
                }
            ];

            Initialized = true;
        }

        #endregion
    }
}

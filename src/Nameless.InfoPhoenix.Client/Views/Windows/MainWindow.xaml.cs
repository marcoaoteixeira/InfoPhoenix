using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Domain.Objects;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Objects;
using Nameless.InfoPhoenix.UI.MessageBox;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows {
    public partial class MainWindow : INavigationWindow {
        #region Private Read-Only Fields

        private readonly IAppConfigurationContext _appConfigurationContext;
        private readonly IMessageBoxService _messageBoxService;
        private readonly INavigationService _navigationService;
        private readonly IPubSubService _pubSubService;
        private readonly IPageService _pageService;
        private readonly ISnackbarService _snackbarService;

        #endregion

        #region Private Fields

        private bool _initialized;

        #endregion

        #region Public Properties

        public MainWindowViewModel ViewModel { get; }

        #endregion

        #region Public Constructors

        public MainWindow(MainWindowViewModel viewModel,
            IAppConfigurationContext appConfigurationContext,
            IMessageBoxService messageBoxService,
            INavigationService navigationService,
            IPubSubService pubSubService,
            IPageService pageService,
            ISnackbarService snackbarService,
            ILogger<MainWindow> logger) {
            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));

            _appConfigurationContext = Guard.Against.Null(appConfigurationContext, nameof(appConfigurationContext));
            _messageBoxService = Guard.Against.Null(messageBoxService, nameof(messageBoxService));
            _navigationService = Guard.Against.Null(navigationService, nameof(navigationService));
            _pubSubService = Guard.Against.Null(pubSubService, nameof(pubSubService));
            _pageService = Guard.Against.Null(pageService, nameof(pageService));
            _snackbarService = Guard.Against.Null(snackbarService, nameof(snackbarService));

            try { InitializeComponent(); }
            catch (Exception ex) { logger.LogError(ex, ex.Message); }

            try { Initialize(); }
            catch (Exception ex) { logger.LogError(ex, ex.Message); }
        }

        #endregion

        #region Private Static Methods

        private static SnackbarInfo CreateSnackbarInfo(SnackbarNotification notification)
            => notification.Severity switch {
                Severity.Information => new SnackbarInfo(notification.Title ?? "Informação", notification.Message, ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info28)),
                Severity.Error => new SnackbarInfo(notification.Title ?? "Erro", notification.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ThumbDislike24)),
                Severity.Success => new SnackbarInfo(notification.Title ?? "Sucesso", notification.Message, ControlAppearance.Success, new SymbolIcon(SymbolRegular.ThumbLike48)),
                Severity.Warning => new SnackbarInfo(notification.Title ?? "Aviso", notification.Message, ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning28)),
                _ => new SnackbarInfo(notification.Title ?? "Notificação", notification.Message, ControlAppearance.Secondary, new SymbolIcon(SymbolRegular.HandWave24)),
            };

        #endregion

        #region Private Methods

        private void Initialize() {
            if (_initialized) { return; }

            SystemThemeWatcher.Watch(this);

            if (Enum.TryParse<ApplicationTheme>(_appConfigurationContext.Theme, out var theme)) {
                ApplicationThemeManager.Apply(theme);
            }

            _navigationService.SetNavigationControl(rootNavigationView);
            _snackbarService.SetSnackbarPresenter(rootSnackbarPresenter);

            _pubSubService.Subscribe<SnackbarNotification>(this, (_, notification) => SnackbarNotificationHandler(notification));
            _pubSubService.Subscribe<CollectDocumentNotification>(this, (_, notification) => StatusTextBlockNotificationHandler(notification));
            
            SetPageService(_pageService);

            _initialized = true;
        }

        private void SnackbarNotificationHandler(SnackbarNotification notification)
            => Dispatcher.InvokeAsync(() => {
                var snackbarInfo = CreateSnackbarInfo(notification);

                _snackbarService
                    .Show(snackbarInfo.Title,
                          snackbarInfo.Content,
                          snackbarInfo.Appearance,
                          snackbarInfo.Icon,
                          UI.Root.Defaults.SnackbarTimeout);
            });

        private void StatusTextBlockNotificationHandler(Notification notification)
            => Dispatcher.Invoke(() => statusTextBlock.Text = notification.Message);

        #endregion

        #region Protected Override Methods

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);

            Navigate(typeof(SearchPage));
        }

        #endregion

        #region Private Records

        private sealed record SnackbarInfo(string Title, string Content, ControlAppearance Appearance, IconElement Icon);

        #endregion

        #region INavigationWindow Members

        public INavigationView GetNavigation()
            => rootNavigationView;

        public bool Navigate(Type pageType)
            => rootNavigationView.Navigate(pageType);

        public void SetServiceProvider(IServiceProvider serviceProvider) { }

        public void SetPageService(IPageService pageService)
            => rootNavigationView.SetPageService(pageService);

        public void ShowWindow()
            => Show();

        public void CloseWindow()
            => Close();

        #endregion

        private void ClosingHandler(object? sender, CancelEventArgs args) {
            if (!_appConfigurationContext.ConfirmBeforeExit) {
                return;
            }

            var result = _messageBoxService.Show(title: "Sair",
                                                 message: "Sempre perguntar ao sair do aplicativo?",
                                                 buttons: UI.MessageBox.MessageBoxButton.YesNoCancel,
                                                 icon: MessageBoxIcon.Question,
                                                 owner: this);

            if (result == UI.MessageBox.MessageBoxResult.No) {
                _appConfigurationContext.ConfirmBeforeExit = false;
            }

            args.Cancel = result == UI.MessageBox.MessageBoxResult.Cancel;
        }
    }
}